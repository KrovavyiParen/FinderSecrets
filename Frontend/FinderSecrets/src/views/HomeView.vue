<template>
  <main>
    <el-input
    v-model="textarea"
    type="textarea"
    placeholder="Input"
    class="in"
  />
  <div>
    <el-button size="large" type="info" plain  round @click="sendText">          
       'Отправить'
    </el-button>
  </div>
  <div v-if="result" class="results-section">
      <h3>Результат:</h3>
      <div class="stats">
        <span class="stat valid">{{ result }}</span>
      </div>
      
      </div>    
  </main>
</template>

<!------------------------------------------------------------------------------------------------------------->

<script setup>
  import { ref} from 'vue'
  import axios from '../../node_modules/axios/dist/axios.min.js'
  
  const textarea = ref('')
  const loading = ref(false)
  const result = ref('')
  
const sendText = async () => {
  loading.value = true
  result.value = ''

  try {
    const response = await axios.post('http://localhost:5200/api/SecretsFinder/scan-text', {
      text: textarea.value
    })
    
    result.value = response.data.processedText || response.data.message
    
  } catch (error) {
    result.value = 'Ошибка: ' + (error.response?.data?.message || error.message)
  } finally {
    loading.value = false
  }
}


  

</script>

<!------------------------------------------------------------------------------------------------------------->


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
