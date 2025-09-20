FROM node:alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

ARG ENV=production

RUN npx ngcc --properties es2023 browser module main --first-only --create-ivy-entry-points
COPY . .
RUN npm run build -- --configuration=${ENV}

FROM nginx:stable

COPY default.conf /etc/nginx/conf.d

COPY --from=build /app/dist/browser /usr/share/nginx/html

COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]
CMD ["nginx", "-g", "daemon off;"]
