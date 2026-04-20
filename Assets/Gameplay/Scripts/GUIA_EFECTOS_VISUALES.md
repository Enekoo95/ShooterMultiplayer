# 🎮 Guía: Configurar Efectos Visuales de Disparo

## ✅ Lo que se implementó

Se agregaron **efectos visuales completos** al sistema de armas:
- ✅ **Sonido de disparo** (AudioClip sincronizado)
- ✅ **Muzzle Flash** (efecto de partículas dinámico)
- ✅ **Camera Recoil** (retroceso suave de cámara)

---

## 🎯 Paso 1: Configurar en Unity (PlayerPrefab)

### 1.1 Abre el PlayerPrefab
1. En Project > Assets > Prefabs
2. Haz doble clic en **PlayerPrefab.prefab** (o drag a la escena)
3. En el Inspector, busca el componente **WeaponSystem**

### 1.2 Asignar AudioClip para Sonido de Disparo
1. En WeaponSystem, encuentra el campo **Shot Sound** (está vacío por defecto)
2. Necesitas un AudioClip:
   - **Opción A**: Usa un sonido existente del proyecto
   - **Opción B**: Crea uno nuevo:
     - Coloca un archivo `.wav` o `.mp3` en Assets/Sounds (crea carpeta si no existe)
     - Arrastra el audio al campo **Shot Sound**

**Ejemplo de sonidos típicos para shooter:**
- Pistola: disparo seco y corto (~100ms)
- Escopeta: sonido más fuerte y profundo
- Rifle: sonido nítido y agudo

### 1.3 Configurar Parámetros de Recoil (Opcional)
En WeaponSystem, ajusta:

| Campo | Valor Defecto | Rango Recomendado | Qué hace |
|-------|--------------|------------------|----------|
| **Recoil Amount** | 0.05 | 0.02 - 0.15 | Qué tan fuerte es el retroceso |
| **Recoil Duration** | 0.1 | 0.05 - 0.3 | Cuánto tarda en recuperarse |

**Tips:**
- Valores más altos = retroceso más dramático
- Para armas de fuego rápida: recoil pequeño (0.02-0.05)
- Para armas pesadas: recoil grande (0.1-0.15)

### 1.4 Habilitar/Deshabilitar Efectos (Opcional)
En WeaponSystem, tienes 3 checkboxes:

```
☑ Enable Muzzle Flash      → Destello visual de cañón
☑ Enable Shot Sound         → Sonido de disparo
☑ Enable Camera Recoil      → Retroceso de cámara
```

Deja todos habilitados para la experiencia completa.

---

## 🔧 Paso 2: Verificar Configuración de Audio

1. **Asegúrate que el jugador tenga AudioSource**:
   - En PlayerPrefab, busca componente **AudioSource**
   - Si no existe, WeaponSystem lo crea automáticamente
   - Verificar que **Play On Awake** esté **OFF**

2. **Niveles de volumen**:
   - AudioSource volume: 0.7 - 1.0 (recomendado 0.8)
   - Shot Sound asignado con volumen normal

---

## 🎮 Paso 3: Probar en Play Mode

1. **Presiona Play en Unity**
2. **Dispara con Mouse Button 0 (Clic Izquierdo)**
3. Deberías escuchar:
   - ✅ Sonido de disparo (si asignaste AudioClip)
   - ✅ Ver flash de luz amarilla (muzzle flash)
   - ✅ Sentir retroceso suave de cámara (se mueve ligeramente)

**Troubleshooting:**
- ❌ No escuchas sonido → Verifica que Shot Sound esté asignado y volumen > 0
- ❌ No ves muzzle flash → Verifica que Enable Muzzle Flash esté ☑
- ❌ No sientes retroceso → Verifica que Enable Camera Recoil esté ☑

---

## 🎨 Personalizaciones Avanzadas

### Crear sonidos distintos por arma
Si quieres que cada arma tenga su propio sonido, modifica **WeaponSystem.cs**:

```csharp
// En el método OnShot(), antes de PlayShotSound()
// Puedes condicionar el sonido por arma actual:

private void OnShot()
{
    // Usar sonido distinto según arma
    switch(currentWeapon.weaponName)
    {
        case "Pistola de Pintura":
            PlayShotSound(highPitchSound); // Sonido agudo
            break;
        case "Escopeta de Pintura":
            PlayShotSound(deepSound);     // Sonido profundo
            break;
        // ... etc
    }
}
```

### Aumentar intensidad de retroceso para armas pesadas
En **WeaponStats**, podrías agregar un campo adicional:

```csharp
public class WeaponStats
{
    public string weaponName;
    public int damage;
    public float fireRate;
    public int ammo;
    public int maxAmmo;
    public WeaponRarity rarity;
    public float recoilMultiplier = 1.0f;  // NUEVO
}
```

Luego en ApplyCameraRecoil():
```csharp
float finalRecoil = recoilAmount * currentWeapon.recoilMultiplier;
```

---

## 📊 Estructura Técnica (Para Referencia)

```
PlayerPrefab
  └─ PlayerController
  └─ WeaponSystem                    ← Controla disparo + efectos
       ├─ Shot Sound (AudioClip)     ← Sonido de disparo
       ├─ Recoil Amount (float)      ← Magnitud retroceso
       ├─ Recoil Duration (float)    ← Duración retroceso
       └─ Enable Flags (3 checkboxes)

     Cuando dispara:
       1. PlayShotSound() → audioSource.PlayOneShot()
       2. PlayMuzzleFlash() → MuzzleFlashEffect.CreateMuzzleFlash()
          └─ Crea objeto temporal con ParticleSystem + Light
       3. ApplyCameraRecoil() → StartCoroutine()
          └─ Mueve cámara suavemente y la devuelve
```

---

## ✅ Checklist de Configuración

- [ ] PlayerPrefab está en la escena o cargará al jugar
- [ ] Shot Sound asignado en WeaponSystem.shotSound
- [ ] AudioSource existe en el PlayerPrefab (o se crea automático)
- [ ] Todos los checkboxes Enable están ☑
- [ ] Valores de recoil configurados según preferencia (0.02-0.15)
- [ ] Probaste en Play Mode y escuchaste/viste efectos
- [ ] El disparo se sincroniza correctamente en multiplayer

---

## 🚀 Próximos Pasos (Opcionales)

Si quieres expandir después:
1. **Agregar variedad de sonidos** - Sonidos diferentes por arma
2. **Ajustar VFX** - Cambiar colores/intensidad de muzzle flash
3. **Animaciones de recarga** - Movimiento visual de cámara al recargar
4. **Impactos visuales** - Efectos cuando golpeas enemigos
5. **Sistema de proyectiles** - Cambiar de raycast a balas físicas

