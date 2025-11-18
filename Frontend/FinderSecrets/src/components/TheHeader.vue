<template>
  <el-header class="main-header">
    <h1>
      Findler
    </h1>
    <div class="bt_in">
      <RouterLink to="/"><el-button size="large" type="info" plain round class="bt">Поиск токена</el-button></RouterLink>
      <RouterLink to="/history"><el-button size="large" type="info" plain round class="bt">История</el-button></RouterLink>
    </div>
    <div class="bt_in">
      <!-- Кнопки входа/регистрации - показываем только когда НЕ авторизован -->
      <div v-if="!isAuthenticated">
        <RouterLink to="/login"><el-button size="large" type="info" plain round class="bt">Войти</el-button></RouterLink>
        <RouterLink to="/registr"><el-button size="large" type="info" plain round class="bt">Регистрация</el-button></RouterLink>
      </div>
      
      <!-- Информация пользователя и кнопка выхода - показываем только когда авторизован -->
      <div v-else class="user-section">
        <span class="user-email">{{ userEmail }}</span>
        <el-button @click="logout" size="large" type="danger" plain round class="bt">Выйти</el-button>
      </div>
    </div>
  </el-header>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { RouterLink, useRouter } from 'vue-router'
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
h1 {
  color: black;
}

.bt_in {
  display: flex;
  align-items: center;
}

.bt {
  margin-left: 5px;
  margin-right: 5px;
}

.main-header {
  height: 100px;
  width: 100%;
  border: 1px solid black;
  border-radius: 20px;
  color: white;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 20px;
}

.user-section {
  display: flex;
  align-items: center;
  gap: 15px;
}

.user-email {
  color: #606266;
  font-size: 14px;
  font-weight: 500;
}
</style>