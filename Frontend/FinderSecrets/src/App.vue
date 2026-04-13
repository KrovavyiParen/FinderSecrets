<script setup>
import { ref, onMounted } from 'vue'
import { RouterView } from 'vue-router'
import TheHeader from "./components/TheHeader.vue"
import TheFooter from "./components/TheFooter.vue"

const isAuthorized = ref(false)
const isLoading = ref(true)

const checkAuth = async () => {
  try {
    const response = await fetch('/api/SecretsFinder/health', {
      method: 'GET',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json'
      }
    })
    
    if (response.ok) {
      isAuthorized.value = true
      return true
    } else if (response.status === 401) {
      const retryResponse = await fetch('/api/SecretsFinder/health', {
        method: 'GET',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json'
        }
      })
      
      if (retryResponse.ok) {
        isAuthorized.value = true
        return true
      }
    }
    
    return false
  } catch (err) {
    console.error('Ошибка проверки авторизации:', err)
    return false
  }
}

onMounted(async () => {
  isLoading.value = true
  await new Promise(resolve => setTimeout(resolve, 100))
  
  const authorized = await checkAuth()
  
  if (!authorized) {
    try {
      const response = await fetch('/api/SecretsFinder/health', {
        method: 'GET',
        credentials: 'include'
      })
      
      if (response.status === 401) {
        setTimeout(() => {
          window.location.reload()
        }, 500)
      }
    } catch (err) {
      console.error('Ошибка:', err)
    }
  }
  
  isLoading.value = false
})
</script>

<template>
  <div v-if="isLoading" class="loading-container">
    <div class="loading-spinner">
      <el-icon class="is-loading"><Loading /></el-icon>
      <span>Загрузка...</span>
    </div>
  </div>
  
  <el-main v-else-if="isAuthorized" class="elmain">
    <TheHeader/>
    <RouterView />
    <TheFooter/>
  </el-main>
  
  <div v-else class="unauthorized-container">
    <el-result
      icon="warning"
      title="Требуется авторизация"
      sub-title="Пожалуйста, введите логин и пароль"
    >
      <template #extra>
        <el-button type="primary" @click="() => { window.location.reload() }">
          Попробовать снова
        </el-button>
      </template>
    </el-result>
  </div>
</template>

<style scoped>
.elmain {
  min-height: 100vh;
  padding: 0;
}

.loading-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
}

.loading-spinner {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  font-size: 16px;
  color: #409eff;
}

.unauthorized-container {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background-color: #f5f7fa;
}
</style>