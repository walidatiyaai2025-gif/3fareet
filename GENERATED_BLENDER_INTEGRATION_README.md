# 3FAREET Generated Integration Pack

Classification: GENERATED_INTEGRATION_CANDIDATE

This package exists so programmers can integrate and exercise the complete
vertical slice together: Hero + 3 Rivals + Cairo Night environment + lighting
reference + Blender-side VFX source meshes.

Do NOT silently rename these files into Production/ or mark external asset
requests ACCEPTED. The current project policy requires generated/procedural
art to remain clearly classified until the acceptance policy is explicitly
changed or a production gate accepts it.

Suggested programmer workflow:
1. Import this folder under Assets/Afareet/GeneratedIntegration/.
2. Build temporary prefabs and runtime bindings against these deterministic files.
3. Validate scale, forward axis (+Y authoring / Unity import normalization), LODs,
   materials, colliders, camera clearance and mobile performance.
4. Keep Production/ fallbacks isolated from GeneratedIntegration/.
5. Use FullSceneReference only as a composition/layout reference; gameplay
   route/control points remain authoritative in code.
