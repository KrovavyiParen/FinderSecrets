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
        
        <!-- Панель поиска и фильтров -->
        <div class="table-controls">
          <div class="search-filter-row">
            <el-input
              v-model="searchQuery"
              placeholder="Поиск по всем полям..."
              clearable
              style="width: 300px; margin-right: 15px;"
              @clear="clearSearch"
              @input="handleSearch"
            >
              <template #prefix>
                <el-icon><Search /></el-icon>
              </template>
            </el-input>

            <el-select
              v-model="activeFilter"
              placeholder="Статус активности"
              clearable
              style="width: 200px; margin-right: 15px;"
              @change="handleFilterChange"
            >
              <el-option label="Активные" value="active" />
              <el-option label="Неактивные" value="inactive" />
              <el-option label="Все" value="all" />
            </el-select>

            <el-select
              v-model="typeFilter"
              placeholder="Тип секрета"
              clearable
              style="width: 200px;"
              @change="handleFilterChange"
            >
              <el-option
                v-for="type in availableTypes"
                :key="type"
                :label="type"
                :value="type"
              />
            </el-select>
          </div>

          <div class="filter-info" v-if="hasActiveFilters">
            <el-tag type="info" closable @close="clearAllFilters">
              Применены фильтры: {{ activeFiltersText }}
            </el-tag>
            <span class="filtered-count">
              Показано: {{ filteredTableData.length }} из {{ tableData.length }}
            </span>
          </div>
        </div>

        <!-- Таблица -->
        <el-table 
          :data="paginatedTableData" 
          border 
          style="width: 100%"
          v-loading="loading"
          highlight-current-row
        >
          <el-table-column prop="secretType" label="Тип" sortable>
            <template #header>
              <div class="column-header">
                <span>Тип</span>
                <el-tooltip content="Фильтр по типу применен" v-if="typeFilter">
                  <el-icon style="color: #409EFF; margin-left: 4px;"><Filter /></el-icon>
                </el-tooltip>
              </div>
            </template>
          </el-table-column>
          
          <el-table-column prop="variableName" label="Имя переменной" sortable />
          <el-table-column prop="secretValue" label="Значение секрета" sortable>
            <template #default="scope">
              <span class="truncated-text" :title="scope.row.secretValue">
                {{ truncateText(scope.row.secretValue, 50) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="lineNumber" label="Номер строки" sortable width="120" />
          <el-table-column prop="firstFound" label="Впервые найден" sortable width="180">
            <template #default="scope">
              <span :title="getFullDateTime(scope.row.firstFound)">
                {{ formatDateTime(scope.row.firstFound) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="lastFound" label="В последний раз найден" sortable width="180">
            <template #default="scope">
              <span :title="getFullDateTime(scope.row.lastFound)">
                {{ formatDateTime(scope.row.lastFound) }}
              </span>
            </template>
          </el-table-column>
          <el-table-column prop="isActive" label="Активен" sortable width="100">
            <template #header>
              <div class="column-header">
                <span>Активен</span>
                <el-tooltip content="Фильтр по активности применен" v-if="activeFilter && activeFilter !== 'all'">
                  <el-icon style="color: #409EFF; margin-left: 4px;"><Filter /></el-icon>
                </el-tooltip>
              </div>
            </template>
            <template #default="scope">
              <el-tag :type="getActiveTagType(scope.row.isActive)">
                {{ getActiveText(scope.row.isActive) }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>

        <!-- Пагинация -->
        <div class="pagination-container">
          <el-pagination
            v-model:current-page="currentPage"
            v-model:page-size="pageSize"
            :page-sizes="[10, 20, 50, 100]"
            :total="filteredTableData.length"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="handleSizeChange"
            @current-change="handleCurrentChange"
          />
        </div>
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
import { ref, computed, onMounted } from 'vue'
import axios from '../../node_modules/axios/dist/axios.min.js'
import { Search, Filter } from '@element-plus/icons-vue'

const loading = ref(false)
const result = ref(null)
const currentPage = ref(1)
const pageSize = ref(10)

// Поиск и фильтры
const searchQuery = ref('')
const activeFilter = ref('all')
const typeFilter = ref('')

const sendText = async () => {
  loading.value = true
  result.value = null
  currentPage.value = 1
  clearAllFilters()

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
  currentPage.value = 1
  clearAllFilters()
}

const clearAllFilters = () => {
  searchQuery.value = ''
  activeFilter.value = 'all'
  typeFilter.value = ''
  currentPage.value = 1
}

const clearSearch = () => {
  searchQuery.value = ''
  currentPage.value = 1
}

const handleSearch = () => {
  currentPage.value = 1
}

const handleFilterChange = () => {
  currentPage.value = 1
}

// Функции для форматирования дат
const formatDateTime = (dateString) => {
  if (!dateString || dateString === 'Не указано') {
    return 'Не указано'
  }

  try {
    const date = new Date(dateString)
    
    // Проверка валидности даты
    if (isNaN(date.getTime())) {
      return 'Неверный формат'
    }

    // Форматирование в локальное время пользователя
    return date.toLocaleDateString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    })
  } catch (error) {
    console.error('Ошибка форматирования даты:', error)
    return 'Ошибка формата'
  }
}

const getFullDateTime = (dateString) => {
  if (!dateString || dateString === 'Не указано') {
    return 'Не указано'
  }

  try {
    const date = new Date(dateString)
    
    if (isNaN(date.getTime())) {
      return 'Неверный формат даты'
    }

    // Полное форматирование для тултипа
    return date.toLocaleDateString('ru-RU', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      timeZoneName: 'short'
    })
  } catch (error) {
    return 'Ошибка формата даты'
  }
}

// Основные данные таблицы
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
    isActive: item.isActive || 'Не указано',
    // Добавляем исходные данные для фильтрации
    _original: item
  }))
})

// Уникальные типы секретов для фильтра
const availableTypes = computed(() => {
  const types = new Set()
  tableData.value.forEach(item => {
    if (item.secretType && item.secretType !== "не указано") {
      types.add(item.secretType)
    }
  })
  return Array.from(types).sort()
})

// Отфильтрованные данные
const filteredTableData = computed(() => {
  let filtered = tableData.value

  // Поиск по всем полям
  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    filtered = filtered.filter(item => 
      Object.values(item).some(value => 
        String(value).toLowerCase().includes(query)
      )
    )
  }

  // Фильтр по активности
  if (activeFilter.value && activeFilter.value !== 'all') {
    filtered = filtered.filter(item => {
      if (activeFilter.value === 'active') return item.isActive === true
      if (activeFilter.value === 'inactive') return item.isActive === false
      return true
    })
  }

  // Фильтр по типу
  if (typeFilter.value) {
    filtered = filtered.filter(item => 
      item.secretType === typeFilter.value
    )
  }

  return filtered
})

// Пагинированные данные
const paginatedTableData = computed(() => {
  const startIndex = (currentPage.value - 1) * pageSize.value
  const endIndex = startIndex + pageSize.value
  return filteredTableData.value.slice(startIndex, endIndex)
})

// Информация о примененных фильтрах
const hasActiveFilters = computed(() => {
  return searchQuery.value || (activeFilter.value && activeFilter.value !== 'all') || typeFilter.value
})

const activeFiltersText = computed(() => {
  const filters = []
  if (searchQuery.value) filters.push(`поиск: "${searchQuery.value}"`)
  if (activeFilter.value === 'active') filters.push('активные')
  if (activeFilter.value === 'inactive') filters.push('неактивные')
  if (typeFilter.value) filters.push(`тип: ${typeFilter.value}`)
  return filters.join(', ')
})

// Обработчики изменения пагинации
const handleSizeChange = (newSize) => {
  pageSize.value = newSize
  currentPage.value = 1
}

const handleCurrentChange = (newPage) => {
  currentPage.value = newPage
}

// Вспомогательные функции
const getActiveTagType = (isActive) => {
  if (isActive === true) return 'danger'
  if (isActive === false) return 'success'
  return 'info'
}

const getActiveText = (isActive) => {
  if (isActive === true) return 'Да'
  if (isActive === false) return 'Нет'
  return 'Неизвестно'
}

const truncateText = (text, maxLength) => {
  if (!text || text.length <= maxLength) return text
  return text.substring(0, maxLength) + '...'
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

.table-controls {
  margin: 20px 0;
}

.search-filter-row {
  display: flex;
  align-items: center;
  margin-bottom: 15px;
  flex-wrap: wrap;
  gap: 15px;
}

.filter-info {
  display: flex;
  align-items: center;
  gap: 15px;
  margin-bottom: 15px;
}

.filtered-count {
  font-size: 14px;
  color: #606266;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: center;
}

.column-header {
  display: flex;
  align-items: center;
  justify-content: center;
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

/* Стили для ячеек с датами */
.el-table .cell {
  white-space: nowrap;
}

/* Адаптивность для мобильных устройств */
@media (max-width: 768px) {
  .search-filter-row {
    flex-direction: column;
    align-items: stretch;
  }
  
  .search-filter-row .el-input,
  .search-filter-row .el-select {
    width: 100% !important;
    margin-right: 0 !important;
    margin-bottom: 10px;
  }
  
  .filter-info {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>