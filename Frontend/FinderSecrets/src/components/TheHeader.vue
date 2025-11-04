<template>
  <el-header class="main-header">
    <RouterLink to="/"><el-button size="large" type="info" plain  round class="bt">Поиск токена</el-button></RouterLink>
    <RouterLink to="/history"> <el-button size="large" type="info" plain  round class="bt">История</el-button></RouterLink>
  </el-header>
</template>

<script setup>
import { RouterLink} from 'vue-router'


</script>

<style scoped>

.bt{
    margin-left: 2%;
    margin-right: 2%;
}

.main-header {
  height: 100px;
  width: 100%;
  border: 1px, solid , black;
  border-radius: 20px;
  color: white;
}

</style>