<template>
  <div id="app">
    <nav class="navbar">
      <div class="nav-brand">🐄 SmartCowFarm</div>
      <div class="nav-links">
        <RouterLink to="/" class="nav-link">Dashboard</RouterLink>
        <RouterLink to="/cows" class="nav-link">Cows</RouterLink>
        <RouterLink to="/vaccinations" class="nav-link">Vaccinations</RouterLink>
      </div>
    </nav>
    <main class="main-content">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { RouterLink, RouterView } from 'vue-router'
</script>

<style>
*, *::before, *::after { box-sizing: border-box; }
body { margin: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f3f4f6; color: #111827; }
a { text-decoration: none; }
</style>

<style scoped>
#app { min-height: 100vh; display: flex; flex-direction: column; }
.navbar {
  background: #1e3a5f; color: white; padding: 0 24px;
  display: flex; align-items: center; height: 56px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.2);
}
.nav-brand { font-size: 1.2rem; font-weight: 700; margin-right: 32px; }
.nav-links { display: flex; gap: 8px; }
.nav-link { color: rgba(255,255,255,0.8); padding: 6px 14px; border-radius: 6px; font-size: 0.95rem; transition: background 0.2s; }
.nav-link:hover, .nav-link.router-link-active { background: rgba(255,255,255,0.15); color: white; }
.main-content { flex: 1; }
</style>
