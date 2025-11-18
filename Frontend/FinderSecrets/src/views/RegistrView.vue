<template>
  <main class="register-container">
    <div class="register-form">
      <h1 class="register-title">Регистрация</h1>
      
      <el-form
        ref="registerFormRef"
        :model="registerForm"
        :rules="registerRules"
        label-width="120px"
        class="demo-registerForm"
        :hide-required-asterisk="true"
      >
        <!-- Имя пользователя -->
        <el-form-item label="Имя пользователя" prop="username">
          <el-input
            v-model="registerForm.username"
            placeholder="Введите имя пользователя"
            clearable
          >
            <template #prefix>
              <el-icon><User /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <!-- Email -->
        <el-form-item label="Email" prop="email">
          <el-input
            v-model="registerForm.email"
            placeholder="Введите email"
            clearable
          >
            <template #prefix>
              <el-icon><Message /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <!-- Пароль -->
        <el-form-item label="Пароль" prop="password">
          <el-input
            v-model="registerForm.password"
            type="password"
            placeholder="Введите пароль"
            show-password
            clearable
          >
            <template #prefix>
              <el-icon><Lock /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <!-- Подтверждение пароля -->
        <el-form-item label="Подтверждение" prop="confirmPassword">
          <el-input
            v-model="registerForm.confirmPassword"
            type="password"
            placeholder="Подтвердите пароль"
            show-password
            clearable
          >
            <template #prefix>
              <el-icon><Lock /></el-icon>
            </template>
          </el-input>
        </el-form-item>

        <!-- Соглашение -->
        <el-form-item prop="agreement">
          <el-checkbox v-model="registerForm.agreement">
            Я принимаю <a href="#" class="link">условия использования</a>
          </el-checkbox>
        </el-form-item>

        <!-- Кнопка регистрации -->
        <el-form-item>
          <el-button
            type="primary"
            @click="submitForm(registerFormRef)"
            :loading="loading"
            class="register-button"
          >
            Зарегистрироваться
          </el-button>
        </el-form-item>
      </el-form>

      <div class="login-link">
        Уже есть аккаунт? <RouterLink to="/login"><a href="#" class="link">Войти</a></RouterLink>
      </div>
    </div>
  </main>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { User, Message, Lock } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import axios from 'axios'
import { useRouter } from 'vue-router'

// Роутер для навигации
const router = useRouter()

// Реф для формы
const registerFormRef = ref()

// Состояние загрузки
const loading = ref(false)

// Данные формы
const registerForm = reactive({
  username: '',
  email: '',
  password: '',
  confirmPassword: '',
  agreement: false
})

// Правила валидации
const validateConfirmPassword = (rule, value, callback) => {
  if (value === '') {
    callback(new Error('Пожалуйста, подтвердите пароль'))
  } else if (value !== registerForm.password) {
    callback(new Error('Пароли не совпадают!'))
  } else {
    callback()
  }
}

const validateAgreement = (rule, value, callback) => {
  if (!value) {
    callback(new Error('Необходимо принять условия использования'))
  } else {
    callback()
  }
}

const registerRules = reactive({
  username: [
    { required: true, message: 'Пожалуйста, введите имя пользователя', trigger: 'blur' },
    { min: 3, max: 20, message: 'Длина имени должна быть от 3 до 20 символов', trigger: 'blur' }
  ],
  email: [
    { required: true, message: 'Пожалуйста, введите email', trigger: 'blur' },
    { type: 'email', message: 'Пожалуйста, введите корректный email', trigger: 'blur' }
  ],
  password: [
    { required: true, message: 'Пожалуйста, введите пароль', trigger: 'blur' },
    { min: 6, message: 'Пароль должен быть не менее 6 символов', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, validator: validateConfirmPassword, trigger: 'blur' }
  ],
  agreement: [
    { required: true, validator: validateAgreement, trigger: 'change' }
  ]
})

// Отправка формы
const submitForm = async (formEl) => {
  if (!formEl) return

  try {
    // Валидация формы
    await formEl.validate()
    
    loading.value = true

    // Отправка данных на бэкенд
    const response = await axios.post('http://localhost:5200/api/secretsfinder/register', {
      username: registerForm.username,
      email: registerForm.email,
      password: registerForm.password
    })

    // Успешная регистрация
    ElMessage.success('Регистрация прошла успешно!')
    console.log('Ответ сервера:', response.data)
    
    // Редирект на страницу входа после успешной регистрации
    setTimeout(() => {
      router.push('/login')
    }, 1500)

  } catch (error) {
    // Обработка ошибок
    if (error.response) {
      // Ошибка от сервера
      const errorMessage = error.response.data?.message || 'Произошла ошибка при регистрации'
      ElMessage.error(errorMessage)
    } else if (error.name === 'ValidationError') {
      // Ошибка валидации формы (уже обрабатывается Element Plus)
      console.log('Ошибка валидации формы')
    } else {
      // Другие ошибки (сеть и т.д.)
      ElMessage.error('Ошибка сети или сервера')
    }
    console.error('Ошибка регистрации:', error)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.register-container {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  align-items: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  padding: 20px;
}

.register-form {
  background: white;
  padding: 40px;
  border-radius: 12px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
  width: 100%;
  max-width: 480px;
}

.register-title {
  text-align: center;
  margin-bottom: 30px;
  color: #333;
  font-size: 28px;
  font-weight: 600;
}

.demo-registerForm {
  margin-bottom: 20px;
}

.register-button {
  width: 100%;
  height: 45px;
  font-size: 16px;
  margin-top: 10px;
}

.login-link {
  text-align: center;
  color: #666;
  margin-top: 20px;
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
</style>