// stores/auth.js
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('token'))
  const refreshToken = ref(localStorage.getItem('refreshToken'))
  const user = ref(JSON.parse(localStorage.getItem('user') || 'null'))

  const isAuthenticated = computed(() => !!token.value)

  const login = async (email, password) => {
    try {
      const response = await api.post('/auth/login', { email, password })
      
      token.value = response.data.token
      refreshToken.value = response.data.refreshToken
      user.value = response.data.user

      // Сохраняем в localStorage
      localStorage.setItem('token', token.value)
      localStorage.setItem('refreshToken', refreshToken.value)
      localStorage.setItem('user', JSON.stringify(user.value))

      return response.data
    } catch (error) {
      throw error
    }
  }

  const logout = () => {
    token.value = null
    refreshToken.value = null
    user.value = null
    
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('user')
  }

  return {
    token,
    refreshToken,
    user,
    isAuthenticated,
    login,
    logout
  }
})