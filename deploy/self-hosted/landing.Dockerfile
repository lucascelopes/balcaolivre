FROM node:22-alpine AS build
WORKDIR /app

COPY BalcaoLivreLadingPage/package*.json ./
RUN npm install

COPY BalcaoLivreLadingPage/ ./
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

EXPOSE 3000
CMD ["npm", "run", "start", "--", "-H", "0.0.0.0", "-p", "3000"]
