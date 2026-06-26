# Permisos de Android — Justificacion

Conforme a la constitucion del proyecto (seccion 5 "Seguridad, Secretos y
Cumplimiento" y seccion 9 "Estandares de Calidad"), cada permiso solicitado debe
quedar justificado en documentacion y revisado antes de cada publicacion.

## Permisos declarados

`Platforms/Android/AndroidManifest.xml` **no declara ningun permiso en tiempo de
ejecucion** (`uses-permission`).

Esto cumple el principio de **minimo privilegio** y "Privacidad primero"
(seccion 3): la aplicacion no necesita permisos peligrosos.

## Como accede la aplicacion a los archivos

La lectura de archivos del usuario se realiza mediante:

- El selector de archivos del sistema (Storage Access Framework).
- Content URIs (`content://...`) leidos con `ContentResolver`.

Estos mecanismos **no requieren** permisos como `READ_EXTERNAL_STORAGE`:
el usuario concede acceso puntual al archivo que selecciona, en linea con
Scoped Storage (Android 10+).

## Revision antes de publicar

Antes de cada publicacion, verificar que el manifiesto sigue sin introducir
permisos innecesarios. Si en el futuro se anade algun permiso, debe:

1. Documentarse aqui con su justificacion funcional.
2. Reflejarse en la seccion de Seguridad de los Datos de Google Play Console.
3. Ser coherente con la politica de privacidad (`store-listing/privacy-policy-*.md`).
