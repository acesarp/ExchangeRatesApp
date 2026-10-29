param(
    [Parameter(Mandatory = $false)]
    [string]$Solution = "",

    [Parameter(Mandatory = $false)]
    [string]$Output = ".\dependency-diagram.md",

    [switch]$IncludePrivateMembers,
    [switch]$IncludeTests,
    [switch]$IncludeGenerated,
    [switch]$OpenOutput
)

$ErrorActionPreference = "Stop"

# ============================================================
# Configuration
# ============================================================

$ExcludedDirectories = @(
    "bin",
    "obj",
    ".git",
    ".vs",
    "node_modules",
    "packages"
)

$GeneratedPatterns = @(
    "*.g.cs",
    "*.g.i.cs",
    "*.generated.cs",
    "*.designer.cs",
    "*AssemblyInfo.cs",
    "*GlobalUsings.g.cs"
)

$TestPatterns = @(
    "*.Tests.csproj",
    "*.Test.csproj",
    "*Tests.csproj",
    "*Test.csproj"
)

# ============================================================
# Helpers
# ============================================================

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Escape-MermaidText {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return ""
    }

    return $Text `
        -replace '"', "'" `
        -replace '<', '~' `
        -replace '>', '~' `
        -replace '\{', '(' `
        -replace '\}', ')' `
        -replace '\|', '/' `
        -replace '\r?\n', ' '
}

function Get-SafeId {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return "Unknown"
    }

    $id = $Value -replace '[^a-zA-Z0-9_]', '_'

    if ($id -match '^[0-9]') {
        $id = "_$id"
    }

    return $id
}

function Is-ExcludedPath {
    param([string]$Path)

    $normalized = $Path.Replace('\', '/')

    foreach ($dir in $ExcludedDirectories) {
        if ($normalized -match "/$([regex]::Escape($dir))/") {
            return $true
        }
    }

    return $false
}

function Is-GeneratedFile {
    param([System.IO.FileInfo]$File)

    foreach ($pattern in $GeneratedPatterns) {
        if ($File.Name -like $pattern) {
            return $true
        }
    }

    return $false
}

function Is-TestProject {
    param([System.IO.FileInfo]$Project)

    foreach ($pattern in $TestPatterns) {
        if ($Project.Name -like $pattern) {
            return $true
        }
    }

    return $false
}

function Get-VisibilitySymbol {
    param([string]$Modifiers)

    if ($Modifiers -match '\bpublic\b') {
        return "+"
    }

    if ($Modifiers -match '\bprotected\b') {
        return "#"
    }

    if ($Modifiers -match '\bprivate\b') {
        return "-"
    }

    if ($Modifiers -match '\binternal\b') {
        return "~"
    }

    return "~"
}

function Get-GenericDisplayName {
    param(
        [string]$Name,
        [string]$GenericArguments
    )

    if ([string]::IsNullOrWhiteSpace($GenericArguments)) {
        return $Name
    }

    $args = $GenericArguments.Trim('<', '>')
    return "$Name~$args~"
}

# ============================================================
# Locate solution
# ============================================================

if ([string]::IsNullOrWhiteSpace($Solution)) {
    $solutions = Get-ChildItem -Path . -Filter *.sln -File

    if ($solutions.Count -eq 0) {
        throw "No .sln file was found. Pass -Solution explicitly."
    }

    if ($solutions.Count -gt 1) {
        Write-Host ""
        Write-Host "Multiple solutions found:" -ForegroundColor Yellow

        for ($i = 0; $i -lt $solutions.Count; $i++) {
            Write-Host "[$i] $($solutions[$i].FullName)"
        }

        $selection = Read-Host "Select solution number"
        $Solution = $solutions[[int]$selection].FullName
    }
    else {
        $Solution = $solutions[0].FullName
    }
}

$Solution = (Resolve-Path $Solution).Path
$SolutionDirectory = Split-Path $Solution -Parent
$Output = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Output))

Write-Step "Solution: $Solution"
Write-Step "Output:   $Output"

# ============================================================
# Discover projects
# ============================================================

Write-Step "Discovering projects"

$projectFiles = Get-ChildItem `
    -Path $SolutionDirectory `
    -Recurse `
    -Filter *.csproj `
    -File |
    Where-Object {
        -not (Is-ExcludedPath $_.FullName)
    }

if (-not $IncludeTests) {
    $projectFiles = $projectFiles | Where-Object {
        -not (Is-TestProject $_)
    }
}

$projects = @{}

foreach ($projectFile in $projectFiles) {

    [xml]$xml = Get-Content $projectFile.FullName

    $projectName = $projectFile.BaseName
    $projectDirectory = $projectFile.Directory.FullName

    $projects[$projectName] = [PSCustomObject]@{
        Name       = $projectName
        Path       = $projectFile.FullName
        Directory  = $projectDirectory
        References = @()
        Types      = @()
    }

    $references = @()

    foreach ($itemGroup in $xml.Project.ItemGroup) {
        foreach ($reference in $itemGroup.ProjectReference) {

            if ($null -ne $reference.Include) {

                $referencePath = Join-Path $projectDirectory $reference.Include
                $referenceName = [System.IO.Path]::GetFileNameWithoutExtension($referencePath)

                $references += $referenceName
            }
        }
    }

    $projects[$projectName].References = $references
}

Write-Host "Found $($projects.Count) projects." -ForegroundColor Green

# ============================================================
# Type storage
# ============================================================

$types = @{}
$typeNames = @{}

function Register-Type {
    param(
        [string]$Project,
        [string]$Namespace,
        [string]$Name,
        [string]$Kind,
        [string]$Modifiers,
        [string]$GenericArguments,
        [string]$BaseList,
        [string]$File
    )

    $fullName = if ([string]::IsNullOrWhiteSpace($Namespace)) {
        $Name
    }
    else {
        "$Namespace.$Name"
    }

    if ($types.ContainsKey($fullName)) {
        return $types[$fullName]
    }

    $type = [PSCustomObject]@{
        Project          = $Project
        Namespace        = $Namespace
        Name             = $Name
        FullName         = $fullName
        Kind             = $Kind
        Modifiers        = $Modifiers
        GenericArguments = $GenericArguments
        BaseList         = $BaseList
        File             = $File

        Fields            = @()
        Properties        = @()
        Methods           = @()
        Constructors      = @()

        Relationships     = @()
    }

    $types[$fullName] = $type

    if (-not $typeNames.ContainsKey($Name)) {
        $typeNames[$Name] = @()
    }

    $typeNames[$Name] += $fullName

    return $type
}

# ============================================================
# C# parser
#
# This parser is deliberately more structured than a simple
# "class regex", but does not attempt to replace the compiler.
#
# It understands the most common modern C# declarations.
# ============================================================

function Parse-CSharpFile {
    param(
        [System.IO.FileInfo]$File,
        [string]$ProjectName
    )

    $content = Get-Content $File.FullName -Raw

    # Remove block comments.
    $clean = [regex]::Replace(
        $content,
        '/\*.*?\*/',
        '',
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )

    # Remove line comments.
    $clean = [regex]::Replace(
        $clean,
        '//.*?$',
        '',
        [System.Text.RegularExpressions.RegexOptions]::Multiline
    )

    # --------------------------------------------------------
    # Namespace
    # --------------------------------------------------------

    $namespace = ""

    $namespaceMatch = [regex]::Match(
        $clean,
        '\bnamespace\s+([A-Za-z_][A-Za-z0-9_.]*)'
    )

    if ($namespaceMatch.Success) {
        $namespace = $namespaceMatch.Groups[1].Value
    }

    # --------------------------------------------------------
    # Type declarations
    # --------------------------------------------------------

    $typePattern = @'
(?ms)
(?<attributes>(?:\s*\[[^\]]+\]\s*)*)
(?<modifiers>(?:(?:public|private|protected|internal|abstract|sealed|static|partial|readonly|ref|file|new)\s+)*)
(?<kind>class|interface|record\s+struct|record|struct|enum)
\s+
(?<name>[A-Za-z_][A-Za-z0-9_]*)
(?<generic>\s*<[^>{};]+>)?
(?<primaryctor>\s*\([^;{}]*\))?
\s*
(?:
    :
    (?<bases>[^{]+?)
)?
\s*
\{
'@

    $matches = [regex]::Matches(
        $clean,
        $typePattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnorePatternWhitespace
    )

    foreach ($match in $matches) {

        $kind = $match.Groups["kind"].Value.Trim()
        $name = $match.Groups["name"].Value.Trim()
        $modifiers = $match.Groups["modifiers"].Value.Trim()
        $generic = $match.Groups["generic"].Value.Trim()
        $bases = $match.Groups["bases"].Value.Trim()

        $type = Register-Type `
            -Project $ProjectName `
            -Namespace $namespace `
            -Name $name `
            -Kind $kind `
            -Modifiers $modifiers `
            -GenericArguments $generic `
            -BaseList $bases `
            -File $File.FullName

        # ----------------------------------------------------
        # Extract body by brace matching
        # ----------------------------------------------------

        $start = $match.Index + $match.Length
        $depth = 1
        $position = $start

        while ($position -lt $clean.Length -and $depth -gt 0) {

            $char = $clean[$position]

            if ($char -eq '{') {
                $depth++
            }
            elseif ($char -eq '}') {
                $depth--
            }

            $position++
        }

        if ($depth -ne 0) {
            continue
        }

        $bodyLength = ($position - 1) - $start

        if ($bodyLength -le 0) {
            continue
        }

        $body = $clean.Substring($start, $bodyLength)

        Parse-TypeBody `
            -Type $type `
            -Body $body
    }
}

# ============================================================
# Parse members
# ============================================================

function Parse-TypeBody {
    param(
        [PSCustomObject]$Type,
        [string]$Body
    )

    # --------------------------------------------------------
    # Properties
    # --------------------------------------------------------

    $propertyPattern = @'
(?mx)
^\s*
(?<modifiers>(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|required|new|readonly)\s+)*)
(?<type>[A-Za-z_][A-Za-z0-9_?.<>,\[\]\s]*)
\s+
(?<name>[A-Za-z_][A-Za-z0-9_]*)
\s*
\{
\s*
(?:
    get\s*;
    |
    set\s*;
    |
    init\s*;
    |
    [^{}]
)+
\}
'@

    foreach ($match in [regex]::Matches(
        $Body,
        $propertyPattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnorePatternWhitespace
    )) {

        $member = [PSCustomObject]@{
            Name       = $match.Groups["name"].Value.Trim()
            Type       = $match.Groups["type"].Value.Trim()
            Modifiers  = $match.Groups["modifiers"].Value.Trim()
        }

        $Type.Properties += $member
    }

    # --------------------------------------------------------
    # Fields
    # --------------------------------------------------------

    $fieldPattern = @'
(?mx)
^\s*
(?<modifiers>(?:(?:public|private|protected|internal|static|readonly|const|volatile|new)\s+)+)
(?<type>[A-Za-z_][A-Za-z0-9_?.<>,\[\]\s]*)
\s+
(?<name>_[A-Za-z_][A-Za-z0-9_]*|[A-Za-z_][A-Za-z0-9_]*)
\s*
(?:=[^;]*)?
;
'@

    foreach ($match in [regex]::Matches(
        $Body,
        $fieldPattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnorePatternWhitespace
    )) {

        $member = [PSCustomObject]@{
            Name       = $match.Groups["name"].Value.Trim()
            Type       = $match.Groups["type"].Value.Trim()
            Modifiers  = $match.Groups["modifiers"].Value.Trim()
        }

        $Type.Fields += $member
    }

    # --------------------------------------------------------
    # Constructors
    # --------------------------------------------------------

    $constructorPattern = "(?mx)^\s*(?<modifiers>(?:(?:public|private|protected|internal|static)\s+)*)$([regex]::Escape($Type.Name))\s*\((?<params>[^)]*)\)"

    foreach ($match in [regex]::Matches($Body, $constructorPattern)) {

        $constructor = [PSCustomObject]@{
            Name       = $Type.Name
            Parameters = $match.Groups["params"].Value.Trim()
            Modifiers  = $match.Groups["modifiers"].Value.Trim()
        }

        $Type.Constructors += $constructor
    }

    # --------------------------------------------------------
    # Methods
    # --------------------------------------------------------

    $methodPattern = @'
(?mx)
^\s*
(?<modifiers>(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|extern|new|partial)\s+)*)
(?<return>[A-Za-z_][A-Za-z0-9_?.<>,\[\]\s]*)
\s+
(?<name>[A-Za-z_][A-Za-z0-9_]*)
(?<generic>\s*<[^>(){};]+>)?
\s*
\(
(?<params>[^)]*)
\)
\s*
(?:where\s+[^{=>;]+)?
\s*
(?:\{|=>|;)
'@

    foreach ($match in [regex]::Matches(
        $Body,
        $methodPattern,
        [System.Text.RegularExpressions.RegexOptions]::IgnorePatternWhitespace
    )) {

        $name = $match.Groups["name"].Value.Trim()

        if ($name -eq $Type.Name) {
            continue
        }

        $method = [PSCustomObject]@{
            Name       = $name
            ReturnType = $match.Groups["return"].Value.Trim()
            Parameters = $match.Groups["params"].Value.Trim()
            Generic    = $match.Groups["generic"].Value.Trim()
            Modifiers  = $match.Groups["modifiers"].Value.Trim()
        }

        $Type.Methods += $method
    }
}

# ============================================================
# Parse source files
# ============================================================

Write-Step "Scanning C# source files"

foreach ($project in $projects.Values) {

    Write-Host "  $($project.Name)" -ForegroundColor DarkCyan

    $files = Get-ChildItem `
        -Path $project.Directory `
        -Recurse `
        -Filter *.cs `
        -File |
        Where-Object {
            -not (Is-ExcludedPath $_.FullName)
        }

    if (-not $IncludeGenerated) {
        $files = $files | Where-Object {
            -not (Is-GeneratedFile $_)
        }
    }

    foreach ($file in $files) {

        try {
            Parse-CSharpFile `
                -File $file `
                -ProjectName $project.Name
        }
        catch {
            Write-Warning "Failed parsing $($file.FullName): $($_.Exception.Message)"
        }
    }
}

Write-Host "Found $($types.Count) declared types." -ForegroundColor Green

# ============================================================
# Resolve type references
# ============================================================

function Find-TypeReferences {
    param([string]$Text)

    $result = @()

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $result
    }

    foreach ($knownType in $typeNames.Keys) {

        if ($Text -match "(?<![A-Za-z0-9_])$([regex]::Escape($knownType))(?![A-Za-z0-9_])") {

            foreach ($fullName in $typeNames[$knownType]) {
                $result += $fullName
            }
        }
    }

    return $result | Select-Object -Unique
}

function Add-Relationship {
    param(
        [PSCustomObject]$Source,
        [string]$Target,
        [string]$Relationship,
        [string]$Label
    )

    if ($Source.FullName -eq $Target) {
        return
    }

    $existing = $Source.Relationships | Where-Object {
        $_.Target -eq $Target -and
        $_.Relationship -eq $Relationship -and
        $_.Label -eq $Label
    }

    if ($null -ne $existing) {
        return
    }

    $Source.Relationships += [PSCustomObject]@{
        Target       = $Target
        Relationship = $Relationship
        Label        = $Label
    }
}

Write-Step "Resolving relationships"

foreach ($type in $types.Values) {

    # --------------------------------------------------------
    # Inheritance / interfaces
    # --------------------------------------------------------

    if (-not [string]::IsNullOrWhiteSpace($type.BaseList)) {

        $bases = $type.BaseList -split ','

        foreach ($base in $bases) {

            $references = Find-TypeReferences $base

            foreach ($reference in $references) {

                $targetType = $types[$reference]

                if ($null -eq $targetType) {
                    continue
                }

                if ($targetType.Kind -eq "interface") {
                    Add-Relationship `
                        -Source $type `
                        -Target $reference `
                        -Relationship "implements" `
                        -Label "implements"
                }
                else {
                    Add-Relationship `
                        -Source $type `
                        -Target $reference `
                        -Relationship "inherits" `
                        -Label "inherits"
                }
            }
        }
    }

    # --------------------------------------------------------
    # Fields = composition/dependency
    # --------------------------------------------------------

    foreach ($field in $type.Fields) {

        foreach ($reference in (Find-TypeReferences $field.Type)) {

            Add-Relationship `
                -Source $type `
                -Target $reference `
                -Relationship "field" `
                -Label $field.Name
        }
    }

    # --------------------------------------------------------
    # Properties
    # --------------------------------------------------------

    foreach ($property in $type.Properties) {

        foreach ($reference in (Find-TypeReferences $property.Type)) {

            Add-Relationship `
                -Source $type `
                -Target $reference `
                -Relationship "property" `
                -Label $property.Name
        }
    }

    # --------------------------------------------------------
    # Method return values
    # --------------------------------------------------------

    foreach ($method in $type.Methods) {

        foreach ($reference in (Find-TypeReferences $method.ReturnType)) {

            Add-Relationship `
                -Source $type `
                -Target $reference `
                -Relationship "returns" `
                -Label $method.Name
        }

        # ----------------------------------------------------
        # Method parameters
        # ----------------------------------------------------

        foreach ($reference in (Find-TypeReferences $method.Parameters)) {

            Add-Relationship `
                -Source $type `
                -Target $reference `
                -Relationship "uses" `
                -Label $method.Name
        }
    }

    # --------------------------------------------------------
    # Constructor injection
    # --------------------------------------------------------

    foreach ($constructor in $type.Constructors) {

        foreach ($reference in (Find-TypeReferences $constructor.Parameters)) {

            Add-Relationship `
                -Source $type `
                -Target $reference `
                -Relationship "depends" `
                -Label "constructor"
        }
    }
}

# ============================================================
# Mermaid generation
# ============================================================

Write-Step "Generating Mermaid"

$builder = [System.Text.StringBuilder]::new()

[void]$builder.AppendLine("# Dependency Diagram")
[void]$builder.AppendLine("")
[void]$builder.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$builder.AppendLine("")
[void]$builder.AppendLine("Solution: ``$([System.IO.Path]::GetFileName($Solution))``")
[void]$builder.AppendLine("")

# ============================================================
# Project dependency diagram
# ============================================================

[void]$builder.AppendLine("## Project Dependencies")
[void]$builder.AppendLine("")
[void]$builder.AppendLine('```mermaid')
[void]$builder.AppendLine("flowchart LR")

foreach ($project in $projects.Values | Sort-Object Name) {

    $projectId = Get-SafeId $project.Name
    $projectLabel = Escape-MermaidText $project.Name

    [void]$builder.AppendLine("    $projectId[`"$projectLabel`"]")
}

foreach ($project in $projects.Values | Sort-Object Name) {

    $from = Get-SafeId $project.Name

    foreach ($reference in $project.References) {

        if (-not $projects.ContainsKey($reference)) {
            continue
        }

        $to = Get-SafeId $reference

        [void]$builder.AppendLine("    $from --> $to")
    }
}

[void]$builder.AppendLine('```')
[void]$builder.AppendLine("")

# ============================================================
# Class diagram
# ============================================================

[void]$builder.AppendLine("## Types")
[void]$builder.AppendLine("")
[void]$builder.AppendLine('```mermaid')
[void]$builder.AppendLine("classDiagram")
[void]$builder.AppendLine("")

foreach ($project in $projects.Values | Sort-Object Name) {

    $projectTypes = $types.Values |
        Where-Object { $_.Project -eq $project.Name } |
        Sort-Object Namespace, Name

    if ($projectTypes.Count -eq 0) {
        continue
    }

    $namespaceId = Get-SafeId $project.Name

    [void]$builder.AppendLine("namespace $namespaceId {")

    foreach ($type in $projectTypes) {

        $id = Get-SafeId $type.FullName
        $displayName = Get-GenericDisplayName `
            -Name $type.Name `
            -GenericArguments $type.GenericArguments

        $displayName = Escape-MermaidText $displayName

        [void]$builder.AppendLine("    class $id {")
        
        switch -Regex ($type.Kind) {
            '^interface$' {
                [void]$builder.AppendLine("        <<interface>>")
            }

            '^record' {
                [void]$builder.AppendLine("        <<record>>")
            }

            '^struct$' {
                [void]$builder.AppendLine("        <<struct>>")
            }

            '^enum$' {
                [void]$builder.AppendLine("        <<enumeration>>")
            }

            default {
                if ($type.Modifiers -match '\babstract\b') {
                    [void]$builder.AppendLine("        <<abstract>>")
                }
            }
        }

        # ----------------------------------------------------
        # Fields
        # ----------------------------------------------------

        foreach ($field in $type.Fields) {

            if (-not $IncludePrivateMembers -and
                $field.Modifiers -match '\bprivate\b') {
                continue
            }

            $visibility = Get-VisibilitySymbol $field.Modifiers
            $fieldType = Escape-MermaidText $field.Type
            $fieldName = Escape-MermaidText $field.Name

            [void]$builder.AppendLine(
                "        $visibility$fieldName : $fieldType"
            )
        }

        # ----------------------------------------------------
        # Properties
        # ----------------------------------------------------

        foreach ($property in $type.Properties) {

            if (-not $IncludePrivateMembers -and
                $property.Modifiers -match '\bprivate\b') {
                continue
            }

            $visibility = Get-VisibilitySymbol $property.Modifiers
            $propertyType = Escape-MermaidText $property.Type
            $propertyName = Escape-MermaidText $property.Name

            [void]$builder.AppendLine(
                "        $visibility$propertyName : $propertyType"
            )
        }

        # ----------------------------------------------------
        # Constructors
        # ----------------------------------------------------

        foreach ($constructor in $type.Constructors) {

            if (-not $IncludePrivateMembers -and
                $constructor.Modifiers -match '\bprivate\b') {
                continue
            }

            $visibility = Get-VisibilitySymbol $constructor.Modifiers
            $parameters = Escape-MermaidText $constructor.Parameters

            [void]$builder.AppendLine(
                "        $visibility$($type.Name)($parameters)"
            )
        }

        # ----------------------------------------------------
        # Methods
        # ----------------------------------------------------

        foreach ($method in $type.Methods) {

            if (-not $IncludePrivateMembers -and
                $method.Modifiers -match '\bprivate\b') {
                continue
            }

            $visibility = Get-VisibilitySymbol $method.Modifiers
            $parameters = Escape-MermaidText $method.Parameters
            $returnType = Escape-MermaidText $method.ReturnType
            $methodName = Escape-MermaidText $method.Name

            [void]$builder.AppendLine(
                "        $visibility$methodName($parameters) $returnType"
            )
        }

        [void]$builder.AppendLine("    }")
    }

    [void]$builder.AppendLine("}")
    [void]$builder.AppendLine("")
}

# ============================================================
# Relationships
# ============================================================

foreach ($type in $types.Values | Sort-Object FullName) {

    $sourceId = Get-SafeId $type.FullName

    foreach ($relationship in $type.Relationships) {

        if (-not $types.ContainsKey($relationship.Target)) {
            continue
        }

        $targetId = Get-SafeId $relationship.Target
        $label = Escape-MermaidText $relationship.Label

        switch ($relationship.Relationship) {

            "inherits" {
                [void]$builder.AppendLine(
                    "$targetId <|-- $sourceId"
                )
            }

            "implements" {
                [void]$builder.AppendLine(
                    "$targetId <|.. $sourceId"
                )
            }

            "field" {
                [void]$builder.AppendLine(
                    "$sourceId --> $targetId : $label"
                )
            }

            "property" {
                [void]$builder.AppendLine(
                    "$sourceId --> $targetId : $label"
                )
            }

            "returns" {
                [void]$builder.AppendLine(
                    "$sourceId ..> $targetId : returns"
                )
            }

            "uses" {
                [void]$builder.AppendLine(
                    "$sourceId ..> $targetId : uses"
                )
            }

            "depends" {
                [void]$builder.AppendLine(
                    "$sourceId ..> $targetId : injects"
                )
            }
        }
    }
}

[void]$builder.AppendLine('```')
[void]$builder.AppendLine("")

# ============================================================
# Statistics
# ============================================================

[void]$builder.AppendLine("## Statistics")
[void]$builder.AppendLine("")

$classCount = ($types.Values | Where-Object { $_.Kind -eq "class" }).Count
$interfaceCount = ($types.Values | Where-Object { $_.Kind -eq "interface" }).Count
$recordCount = ($types.Values | Where-Object { $_.Kind -match "^record" }).Count
$structCount = ($types.Values | Where-Object { $_.Kind -eq "struct" }).Count
$enumCount = ($types.Values | Where-Object { $_.Kind -eq "enum" }).Count

[void]$builder.AppendLine("| Type | Count |")
[void]$builder.AppendLine("|---|---:|")
[void]$builder.AppendLine("| Projects | $($projects.Count) |")
[void]$builder.AppendLine("| Classes | $classCount |")
[void]$builder.AppendLine("| Interfaces | $interfaceCount |")
[void]$builder.AppendLine("| Records | $recordCount |")
[void]$builder.AppendLine("| Structs | $structCount |")
[void]$builder.AppendLine("| Enums | $enumCount |")
[void]$builder.AppendLine("| Total types | $($types.Count) |")

# ============================================================
# Save
# ============================================================

$outputDirectory = Split-Path $Output -Parent

if (-not (Test-Path $outputDirectory)) {
    New-Item `
        -ItemType Directory `
        -Path $outputDirectory `
        -Force | Out-Null
}

[System.IO.File]::WriteAllText(
    $Output,
    $builder.ToString(),
    [System.Text.UTF8Encoding]::new($false)
)

Write-Host ""
Write-Host "Dependency diagram generated successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Output:" -ForegroundColor Cyan
Write-Host $Output
Write-Host ""
Write-Host "Projects:   $($projects.Count)"
Write-Host "Types:      $($types.Count)"
Write-Host "Classes:    $classCount"
Write-Host "Interfaces: $interfaceCount"
Write-Host "Records:    $recordCount"
Write-Host "Structs:    $structCount"
Write-Host "Enums:      $enumCount"

if ($OpenOutput) {
    Invoke-Item $Output
}