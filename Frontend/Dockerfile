FROM node:20-alpine

WORKDIR /app

# Копируем package.json из папки FinderSecrets
COPY FinderSecrets/package*.json ./

# Устанавливаем зависимости
RUN npm install

# Копируем весь код фронтенда из папки FinderSecrets
COPY FinderSecrets/ .

# Собираем приложение
RUN npm run build

# Устанавливаем сервер для статики
RUN npm install -g serve

# Запускаем сервер
CMD ["serve", "-s", "dist", "-l", "3000"]

EXPOSE 3000