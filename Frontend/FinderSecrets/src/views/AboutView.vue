<template>
  <main>
<!-- Область для загрузки файлов -->
    <el-upload
      class="upload-demo"
      drag
      action="#"
      :auto-upload="false"
      :on-change="handleFileChange"
      :on-remove="handleFileRemove"
      :file-list="fileList"
      :limit="1"
      accept=".txt,.log,.json,.xml,.yaml,.yml,.config,.conf,.ini,.env,.properties,.sql,.csv,.tsv,.key,.pem,.ppk,.pub,.cer,.crt,.der,.p12,.pfx,.p7b,.p7c"
    >
      <el-icon class="el-icon--upload"><upload-filled /></el-icon>
      <div class="el-upload__text">
        Перетащите файл сюда или <em>нажмите для загрузки</em>
      </div>
      <template #tip>
        <div class="el-upload__tip">
          Поддерживаемые форматы: текстовые файлы, конфиги, ключи (txt, log, json, xml, yaml, env, key, pem и др.)
        </div>
      </template>
    </el-upload>

    <!-- Показываем содержимое выбранного файла -->
    <div v-if="fileContent && !loading" class="file-preview">
      <h4>Предпросмотр содержимого файла:</h4>
      <el-input
        v-model="fileContent"
        type="textarea"
        readonly
        placeholder="Содержимое файла появится здесь"
        class="file-content-preview"
        :rows="6"
      />
    </div>

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
      
      <div v-if="Array.isArray(result.secrets) && result.secrets.length > 0" class="secrets-found">
        <div class="stats">
          <el-alert title="Внимание!" type="warning" show-icon>
            Найдено потенциальных секретов: {{ result.secrets.length }}
          </el-alert>
        </div>
        
        <div v-for="(secret, index) in result.secrets" :key="index" class="secret-item">
          <div class="secret-header">
            <el-tag type="danger">{{ secret.type }}</el-tag>
            <span class="location">Строка {{ secret.lineNumber }}, позиция {{ secret.position }}</span>
          </div>
          <div class="secret-value">
            <code>{{ secret.variableName }}</code>
          </div>
          <div class="secret-value">
            <code>{{ secret.value }}</code>
          </div>
          <div v-if="secret.type === 'Telegram-Token'" class="secret-value">
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

      <div v-else-if="Array.isArray(result.secrets) && result.secrets.length === 0" class="no-secrets">
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
import { UploadFilled } from '@element-plus/icons-vue'
import axios from '../../node_modules/axios/dist/axios.min.js'

const fileList = ref([])
const fileContent = ref('')
const currentFile = ref(null)
const loading = ref(false)
const result = ref(null)

const handleFileChange = (file) => {
  currentFile.value = file.raw
  
  const reader = new FileReader()
  reader.onload = (e) => {
    try {
      fileContent.value = e.target.result
      result.value = null // Сбрасываем предыдущие результаты
    } catch (error) {
      fileContent.value = ''
      ElMessage.error('Ошибка чтения файла')
    }
  }
reader.onerror = () => {
    ElMessage.error('Ошибка при чтении файла')
    fileContent.value = ''
  }
  
  reader.readAsText(file.raw)
}

// Обработчик удаления файла
const handleFileRemove = () => {
  fileContent.value = ''
  currentFile.value = null
  result.value = null
  fileList.value = []
}

const sendText = async () => {
  if (!fileContent.value) {
    ElMessage.warning('Сначала выберите файл')
    return
  }
  
  loading.value = true
  result.value = null

  try {
    const formData = new FormData()
    formData.append('file', currentFile.value)
    const response = await axios.post('http://localhost:5200/api/secretsfinder/scan-file', formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data'  // ← ВАЖНО!
      }
    })
    result.value = response.data
    
  } catch (error) {
    if (error.response) {
        console.log('Status:', error.response.status);
        console.log('Headers:', error.response.headers);
        console.log('Data:', error.response.data); // Здесь должна быть детальная ошибка от сервера
    }
    result.value = { 
      error: error.response?.data?.message || error.message 
    }
  } finally {
    loading.value = false
  }
}

const clearData = () => {
  // Очищаем файлы
  fileList.value = []
  fileContent.value = ''
  currentFile.value = null
  
  // Очищаем результаты
  result.value = null
  
  ElMessage.success('Все данные очищены')
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