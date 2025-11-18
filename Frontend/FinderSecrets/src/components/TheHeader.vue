<template>
  <header class="header">
    <div class="header-content">
      <div class="logo">
        <RouterLink to="/">Secrets Finder</RouterLink>
      </div>
      <nav class="nav">
        <RouterLink to="/" class="nav-link">Главная</RouterLink>
        <RouterLink to="/history" class="nav-link">История</RouterLink>
        
        <!-- Кнопки входа/регистрации - показываем только когда НЕ авторизован -->
        <div v-if="!isAuthenticated" class="auth-buttons">
          <RouterLink to="/login">
            <el-button type="primary" size="small">Вход</el-button>
          </RouterLink>
          <RouterLink to="/registr">
            <el-button size="small">Регистрация</el-button>
          </RouterLink>
        </div>
        
        <!-- Информация пользователя и кнопка выхода - показываем только когда авторизован -->
        <div v-else class="user-section">
          <span class="user-email">{{ userEmail }}</span>
          <el-button @click="logout" type="danger" size="small" plain>Выйти</el-button>
        </div>
      </nav>
    </div>
  </header>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'

const router = useRouter()

// Реактивные переменные
const isAuthenticated = ref(false)
const userEmail = ref('')

// Функция проверки аутентификации
const checkAuth = () => {
  const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken')
  const userData = localStorage.getItem('userData')
  
  isAuthenticated.value = !!token
  
  if (userData) {
    try {
      const user = JSON.parse(userData)
      userEmail.value = user.email || user.username || 'Пользователь'
    } catch (e) {
      userEmail.value = 'Пользователь'
    }
  } else {
    userEmail.value = 'Пользователь'
  }
}

// Функция выхода
const logout = () => {
  localStorage.removeItem('authToken')
  sessionStorage.removeItem('authToken')
  localStorage.removeItem('userData')
  localStorage.removeItem('userEmail')
  
  ElMessage.success('Вы успешно вышли из системы')
  checkAuth() // Обновляем состояние
  router.push('/')
}

// Слушаем кастомные события для обновления состояния
const handleAuthUpdate = () => {
  checkAuth()
}

// Инициализация при монтировании компонента
onMounted(() => {
  checkAuth()
  // Слушаем кастомное событие для обновления аутентификации
  window.addEventListener('auth-update', handleAuthUpdate)
})

// Очистка при размонтировании
onUnmounted(() => {
  window.removeEventListener('auth-update', handleAuthUpdate)
})
</script>

<style scoped>
.header {
  background: #fff;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  padding: 0 20px;
  border-bottom: 1px solid #e4e7ed;
}

.header-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  max-width: 1200px;
  margin: 0 auto;
  height: 60px;
}

.logo a {
  font-size: 24px;
  font-weight: bold;
  color: #409EFF;
  text-decoration: none;
}

.nav {
  display: flex;
  align-items: center;
  gap: 20px;
}

.nav-link {
  text-decoration: none;
  color: #606266;
  padding: 8px 16px;
  border-radius: 4px;
  transition: all 0.3s;
}

.nav-link:hover {
  color: #409EFF;
  background: #ecf5ff;
}

.nav-link.router-link-active {
  color: #409EFF;
  background: #ecf5ff;
}

.auth-buttons {
  display: flex;
  gap: 10px;
  align-items: center;
}

.user-section {
  display: flex;
  align-items: center;
  gap: 15px;
}

.user-email {
  color: #606266;
  font-size: 14px;
}

/* Убираем подчеркивание у ссылок с кнопками */
.auth-buttons a {
  text-decoration: none;
}
</style>