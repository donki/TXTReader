# Solicitud de acceso a producción en Google Play — TXT Reader

Respuestas para el cuestionario de Play Console › **Panel › Solicitar acceso a producción**, en
catalán (el idioma de la consola). Cada texto cabe en los 300 caracteres del formulario; el número
entre paréntesis es su longitud. Constitución Mobile §11. **Última actualización: 2026-10-01**
(versión 2026.10.02.0). Estado en Play: prueba cerrada (alpha 2026.10.02.0 subida el 2026-10-02; en la pista desde el 2026-09-12).

> Lo marcado con ⚠ no lo puedo saber yo: compruébalo en la consola antes de enviarlo y cámbialo si
> no es así.
>
> - ⚠ La API dice que la pista **production** tiene la 2026.07.28.0 «completed»: comprueba en el Panel si la app ya está en producción; si lo está, el cuestionario no hace falta.

---

## Informació sobre la prova tancada

**Com has reclutat usuaris per a la prova tancada?** (259)

```
He afegit a la prova tancada quatre grups públics de Google de verificadors voluntaris (comunitats d'intercanvi de proves de 12 persones durant 14 dies). No he fet servir cap proveïdor de pagament. També l'he provada jo mateix en un mòbil real amb Android 16.
```

**Fins a quin punt t'ha resultat fàcil reclutar verificadors?** — Propuesta: **Ni fàcil ni difícil** (los grupos públicos dan el número, pero participan poco).

**Descriu la implicació dels verificadors** (240) ⚠ *comprueba en Estadísticas / Prova tancada que de verdad la abrieron; si no hay datos, quita la parte de las funciones.*

```
Els verificadors han instal·lat l'app i l'han oberta amb fitxers de text propis: obrir des del selector i des d'altres apps, cercar, zoom, copiar text i recents. És l'ús que espero d'un usuari real: llegir registres, notes i configuracions.
```

**Resum dels suggeriments i com els has recollit** (269) ⚠ *si algún verificador dejó comentarios (en la consola o por correo), menciónalos.*

```
Pocs comentaris escrits dels verificadors; els he recollit des de la consola de Play i GitHub. Les millores han sortit sobretot de les meves proves en un mòbil real i del banc de proves: fitxers amb accents de Windows, el botó enrere a Android 16 i la cerca de símbols.
```

## Informació sobre l'aplicació

**A quin públic objectiu va dirigida?** (211)

```
Qualsevol persona que necessiti llegir fitxers de text al mòbil (txt, log, json, xml, csv, md, gpx...): desenvolupadors, tècnics, estudiants i ús personal. Sense anuncis, sense compte i sense demanar cap permís.
```

**Com proporciona valor als usuaris?** (254)

```
Obre fitxers de text del mòbil, de Google Drive, de OneDrive o d'altres apps, detecta la codificació sola (accents i €), cerca amb ressaltat, zoom, selecció per copiar i recents. Tot es queda al dispositiu: sense permisos, sense anuncis i amb codi obert.
```

**Instal·lacions esperades el primer any** — Propuesta: **0 - 10.000** (app nueva, sin promoción).

## Preparació per a la producció

**Quins canvis has fet en funció de la prova tancada?** (255)

```
He arreglat el botó enrere a Android 16, he afegit un gestor d'errors perquè cap error la tanqui, ara obre bé els fitxers amb accents i € de Windows i els UTF-32, la cerca troba &, < i cometes, els avisos surten en l'idioma de l'app i he afegit 68 proves.
```

**Com has decidit que està preparada per a producció?** (241) ⚠ *comprueba en Qualitat › Android Vitals que no hay fallos; si los hay, quita «sense tancaments a la consola».*

```
Les 68 proves automàtiques passen totes, l'he provada en un mòbil real amb Android 16 sense errors, els verificadors l'han fet servir 14 dies sense tancaments a la consola i la fitxa, la privadesa i la seguretat de les dades estan completes.
```
