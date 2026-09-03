# Build do React/Vite
FROM node:22-alpine AS build

WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build


# Servir o build com Nginx
FROM nginx:alpine

COPY --from=build /app/dist /usr/share/nginx/html/exchangerates
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]