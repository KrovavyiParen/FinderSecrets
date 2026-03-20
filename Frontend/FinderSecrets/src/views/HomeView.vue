<template>
  <main>
    <!-- Переключатель между текстом и ссылкой -->
    <div class="input-mode-selector">
      <el-radio-group v-model="inputMode" size="large">
        <el-radio-button label="text">Текст</el-radio-button>
        <el-radio-button label="file">Файл</el-radio-button>
      </el-radio-group>
    </div>

    <!-- Поле для ввода текста -->
    <div v-if="inputMode === 'text'" class="input-section">
      <el-input
        v-model="textarea"
        type="textarea"
        placeholder="Введите текст или ссылку для проверки"
        class="in"
        :rows="6"
      />
    </div>

    <div v-else class="input-section">
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
          Перетащите файл сюда или <em> нажмите для загрузки</em>
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
    </div>

    <div>
      <el-button size="large" type="primary" :loading="loading" @click="sendRequest">
        {{ loading ? 'Сканируем' : 'Найти секреты' }}
      </el-button>
      <el-button size="large" @click="clearData" :disabled="loading">
        Очистить
      </el-button>
    </div>

    <!-- Блок результатов -->
    <div v-if="result" class="results-section">
      <!-- Ошибка глобальная -->
      <div v-if="result.error" class="error-result">
        <el-alert :title="'Ошибка: ' + result.error" type="error" show-icon />
      </div>

      <!-- Успешные результаты -->
      <div v-else-if="result.results">
        <!-- Сводка -->
        <div class="summary">
          <el-descriptions :column="3" border>
            <el-descriptions-item label="Источник">{{ result.sourceUrl || 'Не указан' }}</el-descriptions-item>
            <el-descriptions-item label="Всего доменов">{{ result.totalDomainsScanned || 0 }}</el-descriptions-item>
            <el-descriptions-item label="Всего секретов">{{ result.totalSecretsFound || 0 }}</el-descriptions-item>
            <el-descriptions-item label="Время сканирования">{{ result.scanDurationMs ? `${result.scanDurationMs} мс` : '—' }}</el-descriptions-item>
            <el-descriptions-item v-if="result.summary" label="Домены с секретами">{{ result.summary.domainsWithSecrets || 0 }}</el-descriptions-item>
            <el-descriptions-item v-if="result.summary" label="Домены без секретов">{{ result.summary.domainsWithoutSecrets || 0 }}</el-descriptions-item>
          </el-descriptions>

          <!-- Типы секретов (сводка) -->
          <div v-if="result.summary && Object.keys(result.summary.secretTypesSummary).length" class="summary-types">
            <h4>Распределение по типам:</h4>
            <el-space wrap>
              <el-tag v-for="(count, type) in result.summary.secretTypesSummary" :key="type">
                {{ type }}: {{ count }}
              </el-tag>
            </el-space>
          </div>
        </div>

        <!-- Результаты по доменам -->
        <div v-if="result.results.length" class="domains-list">
          <div v-for="(domainResult, idx) in result.results" :key="idx" class="domain-card">
            <div class="domain-header">
              <div>
                <el-link :href="domainResult.url" target="_blank" type="primary">{{ domainResult.domain || domainResult.url }}</el-link>
                <el-tag :type="domainResult.scanStatus === 'Success' ? 'success' : 'danger'" size="small" class="status-tag">
                  {{ domainResult.scanStatus }}
                </el-tag>
              </div>
              <div class="domain-meta">
                <span>Найдено секретов: {{ domainResult.secretsFound }}</span>
                <span v-if="domainResult.scanTime">Сканировано: {{ formatDate(domainResult.scanTime) }}</span>
              </div>
            </div>

            <!-- Ошибка при сканировании домена -->
            <div v-if="domainResult.errorMessage" class="domain-error">
              <el-alert :title="domainResult.errorMessage" type="warning" show-icon :closable="false" />
            </div>

            <!-- Секреты домена -->
            <div v-if="domainResult.secrets && domainResult.secrets.length" class="secrets-list">
              <div v-for="(secret, sIdx) in domainResult.secrets" :key="sIdx" class="secret-item">
                <div class="secret-header">
                  <el-tag type="danger">{{ secret.type }}</el-tag>
                  <el-tag v-if="secret.isActive === true" type="success" size="small">Активен</el-tag>
                  <el-tag v-else-if="secret.isActive === false" type="info" size="small">Неактивен</el-tag>
                </div>
                <div class="secret-value">
                  <div><strong>Переменная:</strong> <code>{{ secret.variableName || '—' }}</code></div>
                  <div><strong>Значение:</strong> <code>{{ secret.value }}</code></div>
                </div>
              </div>
            </div>
            <div v-else-if="domainResult.scanStatus === 'Success'" class="no-secrets">
              <el-alert title="Секреты не найдены" type="success" show-icon :closable="false" />
            </div>
          </div>
        </div>

        <!-- Если results пустой, но общее количество секретов 0 -->
        <div v-else-if="result.totalSecretsFound === 0" class="no-secrets">
          <el-alert title="Секреты не найдены" type="success" show-icon />
        </div>
      </div>
    </div>
  </main>
</template>

<script setup>
import { ref } from 'vue'
import { ElMessage } from 'element-plus'
import { UploadFilled } from '@element-plus/icons-vue'
import api from '@/utils/api'

const inputMode = ref('text')
const textarea = ref('')
const fileList = ref([])
const fileContent = ref('')
const currentFile = ref(null)
const loading = ref(false)
const result = ref(null)

const handleFileChange = (file) => {
  currentFile.value = file.raw
  const reader = new FileReader()
  reader.onload = (e) => {
    fileContent.value = e.target.result
    result.value = null
  }
  reader.onerror = () => {
    ElMessage.error('Ошибка чтения файла')
    fileContent.value = ''
  }
  reader.readAsText(file.raw)
}

const handleFileRemove = () => {
  fileContent.value = ''
  currentFile.value = null
  result.value = null
  fileList.value = []
}

const sendRequest = async () => {
  if (inputMode.value === 'text' && !textarea.value.trim()) {
    ElMessage.warning('Пожалуйста, введите текст или ссылку для проверки')
    return
  }
  if (inputMode.value === 'file' && !fileContent.value) {
    ElMessage.warning('Сначала выберите файл')
    return
  }

  loading.value = true
  result.value = null

  try {
    let response
    if (inputMode.value === 'text') {
      response = await api.post('/secretsfinder/start-scan', { text: textarea.value })
    } else {
      const formData = new FormData()
      formData.append('file', currentFile.value)
      response = await api.post('/secretsfinder/scan-file', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
    }
    result.value = response.data
  } catch (error) {
    result.value = { error: error.response?.data?.message || error.message }
  } finally {
    loading.value = false
  }
}

const clearData = () => {
  textarea.value = ''
  fileList.value = []
  fileContent.value = ''
  currentFile.value = null
  result.value = null
}

const formatDate = (isoString) => {
  if (!isoString) return ''
  const date = new Date(isoString)
  return date.toLocaleString()
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

.in {
  margin-top: 20px;
  margin-bottom: 20px;
  width: 90%;
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

.summary {
  margin-bottom: 20px;
}

.summary-types {
  margin-top: 10px;
}

.domains-list {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.domain-card {
  border: 1px solid #e4e7ed;
  border-radius: 12px;
  padding: 16px;
  background: #fafafa;
}

.domain-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
  flex-wrap: wrap;
  gap: 10px;
}

.domain-meta {
  font-size: 0.85em;
  color: #606266;
  display: flex;
  gap: 15px;
}

.status-tag {
  margin-left: 8px;
  vertical-align: middle;
}

.domain-error {
  margin: 12px 0;
}

.secrets-list {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.secret-item {
  background: #fff;
  border: 1px solid #fcd3d3;
  border-radius: 8px;
  padding: 12px;
}

.secret-header {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 8px;
}

.secret-value {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.secret-value code {
  background: #f5f7fa;
  padding: 4px 8px;
  border-radius: 4px;
  font-family: monospace;
  word-break: break-all;
  display: inline-block;
}

.no-secrets {
  margin-top: 10px;
}

.error-result {
  margin-top: 20px;
}
</style>