<template>
  <main>
    <div>
      <el-button size="large" type="primary" :loading="loading" @click="sendText">          
        {{ loading ? 'Сканируем' : 'Найти историю' }}
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
        
        <el-table :data="tableData" border style="width: 100%">
          <el-table-column prop="secretType" label="Тип" sortable />
          <el-table-column prop="variableName" label="Имя переменной" sortable />
          <el-table-column prop="secretValue" label="Значение секрета" sortable />
          <el-table-column prop="lineNumber" label="Номер строки" sortable />
          <el-table-column prop="firstFound" label="Впервые найден" sortable />
          <el-table-column prop="lastFound" label="В последний раз найден" sortable />
          <el-table-column prop="isActive" label="Активен" sortable>
            <template #default="scope">
              <el-tag :type="getActiveTagType(scope.row.isActive)">
                {{ getActiveText(scope.row.isActive) }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <div v-else-if="Array.isArray(result.items) && result.items.length === 0" class="no-secrets">
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
import { ref, computed } from 'vue'
import axios from '../../node_modules/axios/dist/axios.min.js'

const loading = ref(false)
const result = ref(null)

const sendText = async () => {
  loading.value = true
  result.value = null

  try {
    const response = await axios.get('http://localhost:5200/api/secretsfinder/tokens-history', {})
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

const tableData = computed(() => {
  if (!result.value || !Array.isArray(result.value.items)) {
    return []
  }

  return result.value.items.map(item => ({
    secretType: item.secretType || "не указано",
    variableName: item.variableName || 'Не указано',
    secretValue: item.secretValue || 'Не указано',
    lineNumber: item.lineNumber || 'Не указано',
    firstFound: item.firstFoundAt || 'Не указано',
    lastFound: item.lastFoundAt || 'Не указано',
    isActive: item.isActive || 'Не указано'
  }))
})

// Функция для получения типа тега активности
const getActiveTagType = (isActive) => {
  if (isActive === true) return 'danger'
  if (isActive === false) return 'success'
  return 'info'
}

// Функция для получения текста активности
const getActiveText = (isActive) => {
  if (isActive === true) return 'Да'
  if (isActive === false) return 'Нет'
  return 'Неизвестно'
}
</script>

<style scoped>
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

.truncated-text {
  cursor: help;
  border-bottom: 1px dotted #606266;
}
</style>