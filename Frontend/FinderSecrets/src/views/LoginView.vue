<template>
  <main class="login-container">
    <div class="login-form">
      <h1 class="login-title">Вход в систему</h1>
      
      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="loginRules"
        label-width="120px"
        class="demo-loginForm"
        :hide-required-asterisk="true"
      >
        <!-- Email или Имя пользователя -->
        <el-form-item label="Email" prop="email">
          <el-input
            v-model="loginForm.email"
            placeholder="Введите email или имя пользователя"
            clearable
          >
            <template #prefix>
              <el-icon><User /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <!-- Пароль -->
        <el-form-item label="Пароль" prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="Введите пароль"
            show-password
            clearable
            @keyup.enter="submitForm(loginFormRef)"
          >
            <template #prefix>
              <el-icon><Lock /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <!-- Запомнить меня и Забыли пароль -->
        <el-form-item>
          <div class="login-options">
            <el-checkbox v-model="loginForm.rememberMe">
              Запомнить меня
            </el-checkbox>
            <a href="#" class="link forgot-password">
              Забыли пароль?
            </a>
          </div>
        </el-form-item>

        <!-- Кнопка входа -->
        <el-form-item>
          <el-button
            type="primary"
            @click="submitForm(loginFormRef)"
            :loading="loading"
            class="login-button"
          >
            Войти
          </el-button>
        </el-form-item>
      </el-form>

      <!-- Блок для отображения ошибок сервера -->
      <div v-if="serverError" class="error-message">
        {{ serverError }}
      </div>

      <div class="register-link">
        Нет аккаунта? <RouterLink to="/registr"><a href="#" class="link">Зарегистрироваться</a></RouterLink>
      </div>
    </div>
  </main>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { User, Lock } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'
import { useRouter } from 'vue-router'

// Роутер для навигации
const router = useRouter()

// Реф для формы
const loginFormRef = ref()

// Состояние загрузки
const loading = ref(false)

// Ошибка сервера
const serverError = ref('')

// Данные формы
const loginForm = reactive({
  email: '',
  password: '',
  rememberMe: false
})

// Правила валидации
const loginRules = reactive({
  email: [
    { required: true, message: 'Пожалуйста, введите email или имя пользователя', trigger: 'blur' }
  ],
  password: [
    { required: true, message: 'Пожалуйста, введите пароль', trigger: 'blur' },
    { min: 6, message: 'Пароль должен быть не менее 6 символов', trigger: 'blur' }
  ]
})

// Отправка формы
const submitForm = async (formEl) => {
  if (!formEl) return

  // Сбрасываем предыдущие ошибки
  serverError.value = ''

  try {
    // Валидация формы
    await formEl.validate()
    
    loading.value = true

    // Отправка данных на бэкенд для входа
    const response = await axios.post('http://localhost:5200/api/secretsfinder/login', {
      email: loginForm.email,
      password: loginForm.password
    }, {
      timeout: 10000, // 10 секунд таймаут
      headers: {
        'Content-Type': 'application/json'
      }
    })

    // Успешный вход
    ElMessage.success('Вход выполнен успешно!')
    console.log('Ответ сервера:', response.data)

    // Сохранение токена и данных пользователя
    if (response.data.token) {
      localStorage.setItem('authToken', response.data.token)
      if (loginForm.rememberMe) {
        localStorage.setItem('userEmail', loginForm.email)
      } else {
        sessionStorage.setItem('authToken', response.data.token)
      }
    }

    // Сохранение данных пользователя
    if (response.data.user) {
      localStorage.setItem('userData', JSON.stringify(response.data.user))
    }

    // Редирект на главную страницу или dashboard
    router.push('/')
    
  } catch (error) {
    // Обработка ошибок
    if (error.response) {
      // Ошибка от сервера (500, 400, 401, etc.)
      console.error('Ошибка сервера:', error.response)
      
      if (error.response.status === 500) {
        serverError.value = 'Внутренняя ошибка сервера. Пожалуйста, попробуйте позже.'
        ElMessage.error('Ошибка сервера. Попробуйте позже.')
      } else if (error.response.status === 401) {
        // Неавторизован - неверные учетные данные
        serverError.value = 'Неверный email/пароль'
        ElMessage.error('Неверный email или пароль')
      } else if (error.response.status === 400) {
        // Ошибка валидации на сервере
        const errorMessage = error.response.data?.message || 'Неверные данные'
        serverError.value = errorMessage
        ElMessage.error(errorMessage)
      } else if (error.response.status === 404) {
        // Пользователь не найден
        serverError.value = 'Пользователь с таким email не найден'
        ElMessage.error('Пользователь не найден')
      } else {
        serverError.value = error.response.data?.message || `Ошибка: ${error.response.status}`
        ElMessage.error(serverError.value)
      }
    } else if (error.code === 'ECONNABORTED' || error.message.includes('timeout')) {
      serverError.value = 'Превышено время ожидания ответа от сервера'
      ElMessage.error('Сервер не отвечает. Попробуйте позже.')
    } else if (error.request) {
      // Запрос был сделан, но ответ не получен
      serverError.value = 'Не удалось соединиться с сервером'
      ElMessage.error('Ошибка сети. Проверьте подключение к интернету.')
    } else {
      // Другие ошибки
      serverError.value = 'Произошла непредвиденная ошибка'
      ElMessage.error('Произошла ошибка при входе в систему')
    }
    
    console.error('Ошибка входа:', error)
  } finally {
    loading.value = false
  }
}

// Функция для автоматического заполнения email при rememberMe
const loadSavedEmail = () => {
  const savedEmail = localStorage.getItem('userEmail')
  if (savedEmail) {
    loginForm.email = savedEmail
    loginForm.rememberMe = true
  }
}

// Загружаем сохраненный email при загрузке компонента
loadSavedEmail()
</script>

<style scoped>
.login-container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.login-form {
  background: white;
  padding: 40px;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  width: 100%;
  max-width: 480px;
  position: relative;
}

.login-title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 28px;
  font-weight: 600;
}

.demo-loginForm {
  margin-bottom: 20px;
}

.login-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.forgot-password {
  font-size: 14px;
}

.login-button {
  width: 100%;
  height: 45px;
  font-size: 16px;
  margin-top: 10px;
}

.register-link {
  text-align: center;
  color: #666;
  margin-top: 20px;
}

.error-message {
  background-color: #fef0f0;
  color: #f56c6c;
  padding: 12px;
  border-radius: 4px;
  margin-top: 15px;
  border: 1px solid #fbc4c4;
  text-align: center;
  font-size: 14px;
}

.social-divider {
  position: relative;
  text-align: center;
  margin: 25px 0;
  color: #999;
}

.social-divider::before {
  content: '';
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  height: 1px;
  background: #e0e0e0;
}

.divider-text {
  background: white;
  padding: 0 15px;
  position: relative;
  font-size: 14px;
}

.social-buttons {
  display: flex;
  gap: 12px;
  margin-top: 20px;
}

.social-button {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.social-button.google {
  border-color: #4285f4;
  color: #4285f4;
}

.social-button.google:hover {
  background-color: #f8faff;
  border-color: #3367d6;
}

.social-button.github {
  border-color: #333;
  color: #333;
}

.social-button.github:hover {
  background-color: #f5f5f5;
  border-color: #000;
}

.link {
  color: #409eff;
  text-decoration: none;
}

.link:hover {
  text-decoration: underline;
}

:deep(.el-form-item__label) {
  font-weight: 500;
}

:deep(.el-input) {
  font-size: 14px;
}

:deep(.el-checkbox) {
  margin-bottom: 0;
}
</style>