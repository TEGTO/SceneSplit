FROM node:alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

ARG ENV=production

COPY . .
RUN npm run build -- --configuration=${ENV}

FROM nginx:stable

COPY default.conf /etc/nginx/templates/default.conf

COPY --from=build /app/dist/browser /usr/share/nginx/html

EXPOSE 80

CMD envsubst '$HUB_URL' < /etc/nginx/templates/default.conf > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'