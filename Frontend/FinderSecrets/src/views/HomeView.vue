

<template>
  <main>
    <el-input
    v-model="textarea"
    type="textarea"
    placeholder="Input"
    class="in"
  />
  <div>
    <el-button size="large" type="info" plain  round @click="checkTokens">click</el-button>
  </div>
  <div v-if="tokens.length > 0" class="results-section">
      <h3>📋 Найденные токены ({{ tokens.length }}):</h3>
      <div class="stats">
        <span class="stat valid">Valid: {{ validCount }}</span>
        <span class="stat invalid">Invalid: {{ invalidCount }}</span>
      </div>
      <div class="tokens-list">
        <div 
          v-for="(token, index) in tokens" 
          :key="index"
          :class="['token-item', token.valid ? 'valid' : 'invalid']"
        >
          <div class="token-content">
            <span class="token-prefix">Bot ID:</span>
            <span class="token-bot-id">{{ token.botId }}</span>
            <span class="token-separator">:</span>
            <span class="token-secret">{{ token.secret }}</span>
          </div>
          <span :class="['token-status', token.valid ? 'valid' : 'invalid']">
            {{ token.valid ? '✅ VALID' : '❌ INVALID' }}
          </span>
        </div>
      </div>
    </div>

    <div v-else-if="checked" class="no-results">
      <p>🚫 Токены не найдены</p>
    </div>

    
  </main>
</template>

<script setup>
  import { ref, computed  } from 'vue'
  const textarea = ref('')
  const tokens = ref([])
const checked = ref(false)

const validCount = computed(() => tokens.value.filter(t => t.valid).length)
const invalidCount = computed(() => tokens.value.filter(t => !t.valid).length)

const checkTokens = () => {
  checked.value = true
  
  // Регулярное выражение для поиска Telegram токенов
  const tokenRegex = /(\d{8,10}:[\w_-]{35})/g
  const matches = textarea.value.match(tokenRegex) || []
  
  tokens.value = matches.map(token => {
    const [botId, secret] = token.split(':')
    const valid = validateToken(token)
    
    return {
      token,
      botId,
      secret,
      valid
    }
  })
}

const validateToken = (token) => {
  const parts = token.split(':')
  
  if (parts.length !== 2) {
    return false
  }
  
  const [botId, secret] = parts
  
  // Проверка Bot ID (только цифры, 8-10 символов)
  const isBotIdValid = /^\d{8,10}$/.test(botId)
  
  // Проверка Secret (35 символов: буквы, цифры, _, -)
  const isSecretValid = /^[\w_-]{35}$/.test(secret)
  
  return isBotIdValid && isSecretValid
}

const clearAll = () => {
  textarea.value = ''
  tokens.value = []
  checked.value = false
}

</script>

<style scoped>

  .in{
    margin-top: 20px;
    margin-bottom: 20px;
    width: 90%;
  }
  
  main{
    border: 1px, solid , black;
    border-radius: 20px;
    margin-top: 20px;
    margin-bottom: 20px;
    padding: 0;
    min-height: calc(100vh - 145px - 145px);
  }
</style>
