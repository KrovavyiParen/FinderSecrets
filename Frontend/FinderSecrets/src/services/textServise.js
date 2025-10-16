// src/services/textService.js
import apiClient from './api';

class TextService {
  
  // POST: Отправить текст на сервер и получить ответ
  async sendText(textData) {
    try {
      const response = await apiClient.post('/text/process', textData);
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: this.handleError(error)
      };
    }
  }

  // GET: Получить приветственное сообщение
  async getWelcomeMessage() {
    try {
      const response = await apiClient.get('/text/welcome');
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: this.handleError(error)
      };
    }
  }

  // POST: Отправить текст для анализа
  async analyzeText(text) {
    try {
      const response = await apiClient.post('/text/analyze', { text });
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: this.handleError(error)
      };
    }
  }

  // POST: Конвертировать текст (например, в верхний регистр)
  async convertText(text, conversionType = 'uppercase') {
    try {
      const response = await apiClient.post('/text/convert', {
        text,
        conversionType
      });
      return {
        success: true,
        data: response.data
      };
    } catch (error) {
      return {
        success: false,
        error: this.handleError(error)
      };
    }
  }

  // Обработка ошибок
  handleError(error) {
    if (error.response) {
      return `Ошибка сервера: ${error.response.status} - ${error.response.data?.message || 'Неизвестная ошибка'}`;
    } else if (error.request) {
      return 'Сервер не отвечает. Проверьте подключение.';
    } else {
      return `Ошибка: ${error.message}`;
    }
  }
}

export default new TextService();