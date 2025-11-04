<template>
  <main>
    <div>
      <el-button size="large" type="primary" :loading="loading" @click="sendText">          
        {{ loading ? 'Сканируем' : 'Найти секреты' }}
      </el-button>
      <el-button size="large" @click="clearData" :disabled="loading">
        Очистить
      </el-button>
    </div>

    <div v-if="result" class="results-section">
      <h3>Результаты сканирования:</h3>
      
      <div v-if="Array.isArray(result.items) && result.items.length > 0" class="secrets-found">
        <div class="stats">
          <el-alert title="Внимание!" type="warning" show-icon>
            Найдено потенциальных секретов: {{ result.items.length }}
          </el-alert>
        </div>
        
        <div v-for="(secret, index) in result.items" :key="index" class="secret-item">
          <div class="secret-header">
            <el-tag type="danger">{{ secret.secretType }}</el-tag>
            <span class="location">Строка {{ secret.lineNumber }}, позиция {{ secret.position }}</span>
          </div>
          <div class="secret-value">
            <code>{{ secret.variableName }}</code>
          </div>
          <div class="secret-value">
            <code>{{ secret.secretValue }}</code>
          </div>
          <div v-if="secret.secretType === 'Telegram-Token'" class="secret-value">
            <span>
              Актив:
            </span>
            <code>
              {{ secret.isActive }}
            </code>
            <span>
              Имя бота:
            </span>
            <code>
              {{ secret.botName }}
            </code>
            <span>
              Username:
            </span>
            <code>
              {{ secret.botUsername }}
            </code>
          </div>
        </div>
      </div>

      <div v-else-if="Array.isArray(result.items) && result.secrets.length === 0" class="no-secrets">
        <el-alert title="Отлично!" type="success" show-icon>
          Секреты не найдены
        </el-alert>
      </div>

      <div v-else-if="typeof result === 'string'" class="error-result">
        <el-alert :title="result" type="error" show-icon />
      </div>

      <div v-else-if="result.error" class="error-result">
        <el-alert :title="'Ошибка: ' + result.error" type="error" show-icon />
      </div>
    </div>
  </main>
</template>

<script setup>

import { ref } from 'vue'
import axios from '../../node_modules/axios/dist/axios.min.js'

const inputMode = ref('text')
const loading = ref(false)
const result = ref(null)



const sendText = async () => {



  loading.value = true
  result.value = null

  try {

    
      const response = await axios.get('http://localhost:5200/api/secretsfinder/tokens-history', {
    })
      result.value = response.data
    
  } catch (error) {
    result.value = { 
      error: error.response?.data?.message || error.message 
    }
  } finally {
    loading.value = false
  }

}


const clearData = () => {
  result.value = null
}



</script>

<style scoped>
.upload-demo {
  margin: 20px auto;
  width: 90%;
}

.file-content-preview {
  margin-top: 10px;
  margin-bottom: 20px;
  width: 90%;
}

.file-preview {
  margin: 20px 0;
}

.file-info {
  margin-bottom: 15px;
}

.file-info .el-tag {
  margin-right: 10px;
}

main {
  border: 1px solid #e4e7ed;
  border-radius: 20px;
  margin: 20px auto;
  padding: 20px;
  max-width: 1200px;
  min-height: calc(100vh - 165px - 165px);
}

.results-section {
  margin-top: 20px;
}

.secret-item {
  background: #fef0f0;
  border: 1px solid #fcd3d3;
  border-radius: 8px;
  padding: 16px;
  margin-bottom: 12px;
}

.secret-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.location {
  font-size: 0.9em;
  color: #909399;
}

.secret-value {
  margin: 20px;
}

.secret-value code {
  background: #fff;
  margin: 5px;
  padding: 8px 12px;
  border-radius: 4px;
  border: 1px solid #e4e7ed;
  font-family: 'Courier New', monospace;
  word-break: break-all;
}

.error-result {
  margin-top: 20px;
}
</style>