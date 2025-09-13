FROM node:alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install

ARG ENV

RUN npx ngcc --properties es2023 browser module main --first-only --create-ivy-entry-points
COPY . .
RUN npm run build -- --configuration=${ENV}

FROM nginx:stable
COPY default.conf /etc/nginx/conf.d
COPY --from=build /app/dist/browser /usr/share/nginx/html
EXPOSE 80