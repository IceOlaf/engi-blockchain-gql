FROM node:16
ADD https://github.com/ufoscout/docker-compose-wait/releases/download/2.9.0/wait /wait
RUN chmod +x /wait
WORKDIR /docs
COPY package.json ./
COPY yarn.lock ./
RUN yarn
COPY . .
ENTRYPOINT [ "bash", "-c", "/wait && yarn generate" ]