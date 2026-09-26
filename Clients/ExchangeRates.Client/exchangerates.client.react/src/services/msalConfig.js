export const msalConfig = {
    auth: {
        clientId: "78022d48-23c2-41e0-a1ec-04ee8956f07e",
        authority: "https://login.microsoftonline.com/4bc7b807-f48c-4586-81a3-385fa19fb764",
        redirectUri: window.location.origin + "/exchangerates/"
    }
};

export const loginRequest = {
    scopes: [
        "api://78022d48-23c2-41e0-a1ec-04ee8956f07e/access_as_user"
    ]
};

const token = await instance.acquireTokenSilent({
    ...loginRequest,
    account: accounts[0]
});

const response = await fetch("/api/Admin/secure", {
    headers: {
        Authorization: `Bearer ${token.accessToken}`
    }
});