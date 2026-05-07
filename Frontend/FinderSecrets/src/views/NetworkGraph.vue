<template>
  <div class="graph-container">
    <el-container style="height: 100%">
      <!-- Боковая панель (без изменений) -->
      <el-aside width="320px" style="background: #f5f7fa; padding: 16px; overflow-y: auto; display: flex; flex-direction: column">
        <!-- Форма сканирования -->
        <div class="scan-section">
          <h3>Сканировать домен</h3>
          <el-input
            v-model="domainInput"
            placeholder="например, ozone.ru"
            clearable
            @keyup.enter="scanDomain"
          >
            <template #prepend>https://</template>
          </el-input>
          <el-button
            type="primary"
            :loading="isLoading"
            @click="scanDomain"
            style="margin-top: 12px; width: 100%"
          >
            <el-icon v-if="!isLoading"><Search /></el-icon>
            {{ isLoading ? 'Сканирование...' : 'Сканировать' }}
          </el-button>
          <el-alert
            v-if="errorMessage"
            type="error"
            :closable="true"
            @close="errorMessage = ''"
            style="margin-top: 10px"
          >
            {{ errorMessage }}
          </el-alert>
          <el-divider />
        </div>

        <!-- Информация о текущем сканировании -->
        <div v-if="currentDomain" class="current-info">
          <el-tag type="success" size="large" effect="dark">
            {{ currentDomain }}
          </el-tag>
          <span style="margin-left: 8px; color: #666">Session: {{ sessionId }}</span>
          <el-divider />
        </div>

        <!-- Информация о выбранном узле -->
        <h3>Информация об узле</h3>
        <el-divider />
        <div v-if="selectedNode" class="node-details">
          <el-descriptions :column="1" border size="small">
            <el-descriptions-item label="ID">{{ selectedNode.id }}</el-descriptions-item>
            <el-descriptions-item label="Метка">{{ selectedNode.label }}</el-descriptions-item>
            <el-descriptions-item label="Тип">{{ selectedNode.type }}</el-descriptions-item>
            <el-descriptions-item label="Данные">{{ selectedNode.data || '-' }}</el-descriptions-item>
            <el-descriptions-item v-if="selectedNode.organization" label="Организация">
              {{ selectedNode.organization }}
            </el-descriptions-item>
            <el-descriptions-item label="Уверенность">
              {{ selectedNode.confidence_score ?? '—' }}
            </el-descriptions-item>
          </el-descriptions>
        </div>
        <el-empty v-else description="Выберите узел" />

        <div style="margin-top: auto">
          <h3 style="margin-top: 24px">Сводка</h3>
          <el-divider />
          <el-statistic title="Всего узлов" :value="summary.total_nodes" />
          <el-statistic title="Всего связей" :value="summary.total_edges" />
        </div>
      </el-aside>

      <!-- Основная область с графом -->
      <el-main style="padding: 0; position: relative">
        <div ref="networkContainer" class="network"></div>
        <div class="legend">
          <el-tag v-for="item in legendItems" :key="item.type" :color="item.color" effect="dark" size="small">
            {{ item.label }}
          </el-tag>
        </div>
        <div v-if="isLoading" class="loading-overlay">
          <el-icon class="is-loading" :size="40"><Loading /></el-icon>
        </div>
      </el-main>
    </el-container>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { Network } from 'vis-network'
import { DataSet } from 'vis-data'
import 'vis-network/styles/vis-network.min.css'
import { Search, Loading } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'

const props = defineProps({
  initialData: {
    type: Object,
    default: null
  }
})

const networkContainer = ref(null)
const selectedNode = ref(null)
let network = null

// Состояние сканирования
const domainInput = ref('ozone.ru')
const isLoading = ref(false)
const errorMessage = ref('')
const currentDomain = ref('')
const sessionId = ref(null)

const graphData = ref({
  nodes: [],
  edges: [],
  summary: { total_nodes: 0, total_edges: 0 }
})

const nodeColors = {
  domain: '#409EFF',
  ip: '#67C23A',
  subnet: '#E6A23C'
}

const legendItems = [
  { type: 'domain', label: 'Домен', color: '#409EFF' },
  { type: 'ip', label: 'IP', color: '#67C23A' },
  { type: 'subnet', label: 'Подсеть', color: '#E6A23C' }
]

const edgeStyles = {
  direct: { color: '#848484', width: 2, dashes: false },
  member_of: { color: '#E6A23C', width: 1.5, dashes: [5, 5] },
  via_ip: { color: '#909399', width: 1, dashes: [10, 5] },
  subdomain: { color: '#F56C6C', width: 1.5, dashes: false }
}

const summary = ref({
  total_nodes: 0,
  total_edges: 0
})

async function scanDomain() {
  const domain = domainInput.value.trim()
  if (!domain) {
    errorMessage.value = 'Введите домен'
    return
  }

  isLoading.value = true
  errorMessage.value = ''

  try {
    const response = await fetch(`http://195.209.218.225:8000/api/links/domain/?domain=${encodeURIComponent(domain)}`)
    if (!response.ok) throw new Error(`Ошибка HTTP: ${response.status}`)
    const data = await response.json()
    if (!data.nodes || !data.edges) throw new Error('Некорректный формат данных')

    graphData.value = data
    currentDomain.value = data.domain || domain
    sessionId.value = data.session_id || null
    
    rebuildGraph(data)
    ElMessage.success(`Сканирование завершено. Найдено ${data.summary?.total_nodes || 0} узлов.`)
  } catch (err) {
    console.error('Ошибка сканирования:', err)
    errorMessage.value = err.message
    ElMessage.error(`Ошибка: ${err.message}`)
  } finally {
    isLoading.value = false
  }
}

function buildVisData(raw) {
  const nodes = []
  const edges = []

  raw.nodes.forEach(n => {
    nodes.push({
      id: n.id,
      label: n.label,
      title: `<b>${n.label}</b><br>${n.type}${n.organization ? '<br>Org: ' + n.organization : ''}`,
      color: {
        background: nodeColors[n.type] || '#909399',
        border: '#333',
        highlight: { background: '#FFD04B', border: '#333' }
      },
      font: { color: '#fff', size: 12 },
      shape: n.type === 'subnet' ? 'diamond' : 'dot',
      size: n.type === 'domain' ? 20 : 15,
      ...n
    })
  })

  raw.edges.forEach(e => {
    const style = edgeStyles[e.type] || edgeStyles.direct
    edges.push({
      id: e.id,
      from: e.source,
      to: e.target,
      label: e.label,
      title: `${e.label} (${e.type})`,
      arrows: e.type === 'direct' ? 'to' : undefined,
      color: style.color,
      width: style.width,
      dashes: style.dashes,
      smooth: e.type === 'member_of' ? false : { enabled: true, type: 'curvedCW', roundness: 0.2 }
    })
  })

  summary.value = raw.summary || { total_nodes: nodes.length, total_edges: edges.length }
  return { nodes: new DataSet(nodes), edges: new DataSet(edges) }
}

function rebuildGraph(data) {
  if (!network || !networkContainer.value) {
    initNetwork(data)
    return
  }

  const { nodes, edges } = buildVisData(data)
  network.setData({ nodes, edges })
  
  setTimeout(() => {
    network.fit({ animation: true })
  }, 100)
}

function initNetwork(initialData = null) {
  if (!networkContainer.value) return

  const data = initialData || graphData.value
  const { nodes, edges } = buildVisData(data)
  
  // Определяем размер графа для адаптивных настроек
  const nodeCount = nodes.length
  const edgeCount = edges.length
  const isLargeGraph = nodeCount > 200 || edgeCount > 500

  const options = {
    nodes: {
      borderWidth: isLargeGraph ? 1 : 2,
      shadow: !isLargeGraph,
      font: { 
        color: '#ffffff', 
        size: isLargeGraph ? 10 : 12, 
        background: 'rgba(0,0,0,0.5)' 
      }
    },
    edges: {
      smooth: isLargeGraph ? false : { enabled: true, type: 'dynamic', roundness: 0.5 },
      font: { size: 10, align: 'middle', background: 'white' }
    },
    physics: {
      enabled: true,
      stabilization: {
        enabled: true,
        iterations: isLargeGraph ? 50 : 100,
        updateInterval: isLargeGraph ? 50 : 25,
        fit: true
      },
      barnesHut: {
        gravitationalConstant: isLargeGraph ? -2000 : -8000,
        centralGravity: 0.3,
        springLength: isLargeGraph ? 250 : 150,
        springConstant: isLargeGraph ? 0.001 : 0.04,
        damping: 0.09
      },
      maxVelocity: isLargeGraph ? 5 : 50,
      minVelocity: 0.1,
      timestep: 0.5,
      adaptiveTimestep: true
    },
    interaction: {
      hover: true,
      tooltipDelay: 100,
      zoomView: true,
      dragView: true,
      navigationButtons: true,
      keyboard: true
    },
    // Отключаем улучшенную вёрстку для больших графов
    layout: {
      improvedLayout: !isLargeGraph
    },
    // Кластеризация для очень больших графов (опционально)
    clustering: {
      enabled: nodeCount > 500,
      initialMaxNodes: 100,
      clusterNodeProperties: {
        borderWidth: 3,
        shape: 'database',
        font: { size: 14 }
      }
    }
  }

  network = new Network(networkContainer.value, { nodes, edges }, options)

  // После завершения стабилизации отключаем физику для экономии CPU
  network.on('stabilizationIterationsDone', () => {
    if (isLargeGraph) {
      network.setOptions({ physics: { enabled: false } })
    }
  })

  // При взаимодействии пользователя (перетаскивание) временно включаем физику для больших графов
  if (isLargeGraph) {
    network.on('dragStart', () => {
      network.setOptions({ physics: { enabled: true } })
    })
    network.on('dragEnd', () => {
      setTimeout(() => {
        network.setOptions({ physics: { enabled: false } })
      }, 500)
    })
  }

  network.on('selectNode', (params) => {
    const nodeId = params.nodes[0]
    const node = nodes.get(nodeId)
    selectedNode.value = node
  })

  network.on('deselectNode', () => {
    selectedNode.value = null
  })
}

onMounted(() => {
  if (props.initialData) {
    graphData.value = props.initialData
    currentDomain.value = props.initialData.domain || ''
    sessionId.value = props.initialData.session_id || null
    domainInput.value = currentDomain.value
  }
  initNetwork()
})

watch(() => props.initialData, (newVal) => {
  if (newVal) {
    graphData.value = newVal
    currentDomain.value = newVal.domain || ''
    sessionId.value = newVal.session_id || null
    domainInput.value = currentDomain.value
    rebuildGraph(newVal)
  }
}, { deep: true })
</script>

<style scoped>
/* стили без изменений */
.graph-container { height: 100vh; width: 100%; }
.scan-section { margin-bottom: 8px; }
.current-info { margin-bottom: 8px; }
.node-details { font-size: 14px; }
.network { width: 100%; height: 100%; background: #ffffff; position: relative; }
.legend {
  position: absolute; bottom: 20px; right: 20px; display: flex; gap: 8px;
  background: rgba(255, 255, 255, 0.8); padding: 8px 12px;
  border-radius: 20px; backdrop-filter: blur(4px); z-index: 10;
}
.loading-overlay {
  position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%);
  background: rgba(255, 255, 255, 0.9); padding: 20px; border-radius: 12px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15); z-index: 20;
  display: flex; align-items: center; justify-content: center;
}
</style>