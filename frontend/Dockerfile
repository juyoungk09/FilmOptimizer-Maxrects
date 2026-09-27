# Vite 8 은 Node 20.19+ / 22.12+ 가 필요하다.
# 18-alpine 로는 build 가 실패한다.
FROM node:22-alpine AS build
WORKDIR /app

# lock 파일이 있으니 npm ci 로 정확히 재현한다.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# dist 만 서빙하면 되므로 Node 런타임은 필요 없다.
FROM nginx:1.27-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
