<template>
  <el-header class="main-header">
    <h1>
      Findler
    </h1>
    <div class="bt_in">
      <RouterLink to="/"><el-button size="large" type="info" plain  round class="bt">Поиск токена</el-button></RouterLink>
      <RouterLink to="/history"> <el-button size="large" type="info" plain  round class="bt">История</el-button></RouterLink>
    </div>
    <div class="bt_in">
      <RouterLink to="/login"> <el-button size="large" type="info" plain  round class="bt">Войти</el-button></RouterLink>
      <RouterLink to="/registr"> <el-button size="large" type="info" plain  round class="bt">Регистрация</el-button></RouterLink>
    </div>
  </el-header>
</template>

<script setup>
import { RouterLink} from 'vue-router'


</script>

<style scoped>

h1{
  color: black;
}

.bt_in{
  display: flex;
}

.bt{
    margin-left: 5px;
    margin-right: 5px;
}


.main-header {
  height: 100px;
  width: 100%;
  border: 1px, solid , black;
  border-radius: 20px;
  color: white;
  display: flex;
  justify-content: space-between;
} 

</style>