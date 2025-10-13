import axios from 'axios';

//экземпляр axios с базовыми настройками

const apiClient = axios.create({
    baseURL: process.env.VUE_APP_API_URL || 'https://localhost:7000/api',
    timeout: 10000,
    headers: {
        'Content-Type': 'application/json',
    },
});


apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Интерцептор для обработки ошибок
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Перенаправление на страницу логина при истечении токена
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;