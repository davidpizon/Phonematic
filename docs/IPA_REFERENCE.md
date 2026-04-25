# International Phonetic Alphabet (IPA) Reference

The **International Phonetic Alphabet (IPA)** is a standardized system for representing the sounds of spoken language. Every distinct sound (phoneme) in any human language can be transcribed using IPA symbols. This document explains how sounds are organized and represented, drawing from the comprehensive IPA charts at [jbdowse.com/ipa](https://jbdowse.com/ipa/).

IPA symbols are used in Phonematic's [PhoScript](PHOSCRIPT.md) output format (`.phos` files) as the value of every `ipa` attribute on `<phon>` elements. See also:
- [PHOSCRIPT.md](PHOSCRIPT.md) — PhoScript 1.0 specification showing how IPA atoms are embedded in prosodic markup
- [ARCHITECTURE.md](ARCHITECTURE.md) — how PhoScript fits into the application's output pipeline
- [API.md](API.md) — `PhoScriptWriter` and related API reference

---

## Table of Contents

1. [Overview](#overview)
2. [How to Read IPA Charts](#how-to-read-ipa-charts)
3. [Consonants](#consonants)
   - [Places of Articulation](#places-of-articulation)
   - [Manners of Articulation](#manners-of-articulation)
   - [Voicing](#voicing)
   - [Common Consonant Chart](#common-consonant-chart)
4. [Vowels](#vowels)
   - [Vowel Height](#vowel-height)
   - [Vowel Backness](#vowel-backness)
   - [Lip Rounding](#lip-rounding)
   - [Vowel Chart](#vowel-chart)
5. [Nasal Vowels](#nasal-vowels)
6. [Rarer Consonant Manners](#rarer-consonant-manners)
   - [Lateral Fricatives and Affricates](#lateral-fricatives-and-affricates)
   - [Implosive Stops](#implosive-stops)
   - [Ejective Sounds](#ejective-sounds)
7. [Click Consonants](#click-consonants)
8. [Diacritics and Modifiers](#diacritics-and-modifiers)
9. [Tips for Use](#tips-for-use)

---

## Overview

IPA notation encloses transcriptions in square brackets `[ ]` for phonetic (precise) transcription, or slashes `/ /` for phonemic (broad) transcription. For example:

- The English word *bit* → `/bɪt/` (phonemic) or `[bɪt]` (phonetic)
- The English word *pin* → `/pɪn/` or `[pʰɪn]` (showing aspiration)

Not every symbol combination represents a sound attested in a real language — the IPA covers the full theoretical space of possible human articulations. Coarticulated sounds (such as Arabic emphatic coronals) are generally not covered in their full complexity, and sibilants are less fine-grained than in some specialist works.

---

## How to Read IPA Charts

IPA consonant charts are organized along two axes:

- **Rows** = manner of articulation (how the airflow is shaped)
- **Columns** = place of articulation (where in the vocal tract the sound is made)

Within each cell:
- The **left-hand symbol** is **voiceless** (vocal cords not vibrating)
- The **right-hand symbol** is **voiced** (vocal cords vibrating)
- **Darker gray cells** represent silent or physically impossible articulations

IPA vowel charts are organized along two axes:

- **Rows** = vowel height (how high the tongue is)
- **Columns** = vowel backness (how far back the tongue is)

Within each vowel cell:
- The **left-hand symbol** (white background) is **unrounded**
- The **right-hand symbol** (gray background) is **rounded**

---

## Consonants

### Places of Articulation

The **place of articulation** describes where in the vocal tract the sound is primarily produced:

| Place | Description | Example |
|---|---|---|
| **Bilabial** | Both lips | `p`, `b`, `m` |
| **Labiodental** | Lower lip + upper teeth | `f`, `v` |
| **Linguolabial** | Tongue tip + upper lip | `n̼`, `t̼` |
| **Dental** | Tongue tip + upper teeth | `θ`, `ð` |
| **Alveolar** | Tongue tip + alveolar ridge (just behind teeth) | `t`, `d`, `n`, `s`, `z` |
| **Postalveolar** | Tongue behind alveolar ridge | `ʃ`, `ʒ`, `tʃ`, `dʒ` |
| **Retroflex** | Tongue tip curled back | `ʈ`, `ɖ`, `ʂ`, `ʐ` |
| **Alveolo-palatal** | Tongue blade near hard palate | `ɕ`, `ʑ` |
| **Palatal** | Tongue body + hard palate | `c`, `ɟ`, `j`, `ɲ` |
| **Velar** | Tongue back + soft palate (velum) | `k`, `g`, `ŋ`, `x` |
| **Rounded velar** | Velar with lip rounding | `kʷ`, `gʷ`, `w` |
| **Uvular** | Tongue back + uvula | `q`, `ɢ`, `χ`, `ʁ` |
| **Low uvular** | Between uvular and pharyngeal | `qʌ`, `ɢʌ` (non-standard; see note) |
| **Pharyngeal** | Tongue root + pharynx wall | `ħ`, `ʕ` |
| **Epiglotto-pharyngeal** | Epiglottis + pharynx | `ʜ͜ħ`, `ʢ͜ʕ` |
| **Aryepiglottal** | Aryepiglottal folds | `ʜ`, `ʢ` |
| **Glottal** | Vocal cords | `h`, `ɦ`, `ʔ` |

> **Note on "low uvular":** This is a non-standard category describing sounds similar to the back component of English "dark L", which involves uvularization lower in the throat than typical uvulars. It is denoted with a superscript `ʌ` added to uvular characters (e.g., `qʌ`, `ɢʌ`).

---

### Manners of Articulation

The **manner of articulation** describes how airflow is shaped to produce the sound:

| Manner | Description | Example |
|---|---|---|
| **Nasal** | Complete oral closure; air flows through nose | `m`, `n`, `ŋ` |
| **Stop (Plosive)** | Complete oral closure; air released suddenly | `p`, `b`, `t`, `d`, `k`, `g` |
| **Aspirated Stop** | Stop followed by a puff of air (`ʰ` = voiceless, `ʱ` = voiced) | `pʰ`, `tʰ`, `kʰ` |
| **Affricate** | Stop followed immediately by a fricative at the same place | `ts`, `dz`, `tʃ`, `dʒ` |
| **Fricative** | Narrow constriction creating turbulent airflow | `f`, `v`, `s`, `z`, `ʃ`, `ʒ` |
| **Approximant** | Near-constriction without turbulence | `j`, `w`, `ɹ`, `ʋ` |
| **Lateral Approximant** | Air flows around the sides of the tongue | `l`, `ʎ`, `ʟ` |
| **Tap / Flap** | Very brief single contact | `ɾ` (Spanish *r* in *pero*) |
| **Trill** | Rapid repeated contact (vibration) | `r` (rolled R), `ʙ` (bilabial trill) |
| **Lateral Fricative** | Lateral airflow with friction | `ɬ`, `ɮ` |
| **Lateral Affricate** | Stop + lateral fricative | `tɬ`, `dɮ` |
| **Lateral Flap** | Brief lateral contact | `ɺ` |
| **Fricative Trill** | Trill with added friction | `r̝` |
| **Implosive Stop** | Stop with inward airstream mechanism | `ɓ`, `ɗ`, `ʄ`, `ɠ`, `ʛ` |
| **Ejective** | Stop or fricative with compressed glottal airstream (marked `ʼ`) | `pʼ`, `tʼ`, `kʼ`, `sʼ` |
| **Click** | Ingressive lingual airstream; distinct front and back closures | `ʘ`, `ǀ`, `ǃ`, `ǁ`, `ǂ` |

---

### Voicing

- **Voiceless** sounds are produced without vocal cord vibration. In charts, they appear on the **left** of each cell.
- **Voiced** sounds involve vocal cord vibration. They appear on the **right**.
- Some sounds can carry **breathy voice** (`ʱ`) or **creaky voice** (indicated with diacritics).

---

### Common Consonant Chart

Below is a summary of the most commonly encountered IPA consonants organized by place and manner:

|  | Bilabial | Labiodental | Dental | Alveolar | Postalveolar | Palatal | Velar | Uvular | Glottal |
|---|---|---|---|---|---|---|---|---|---|
| **Nasal** | m | ɱ | n̪ | n | | ɲ | ŋ | ɴ | |
| **Stop** | p b | | t̪ d̪ | t d | | c ɟ | k g | q ɢ | ʔ |
| **Affricate** | | p̪f b̪v | t̪θ d̪ð | ts dz | tʃ dʒ | cç ɟʝ | kx gɣ | qχ ɢʁ | ʔh |
| **Fricative** | ɸ β | f v | θ ð | s z | ʃ ʒ | ç ʝ | x ɣ | χ ʁ | h ɦ |
| **Approximant** | | ʋ | | ɹ | | j | ɰ | | |
| **Lateral approx.** | | | | l | | ʎ | ʟ | | |
| **Tap / Flap** | | | | ɾ | | | | | |
| **Trill** | ʙ | | | r | | | | ʀ | |

---

## Vowels

Vowels are sounds produced with a relatively open vocal tract. They are classified by three primary features:

### Vowel Height

How high the tongue body is raised in the mouth:

| Height | Description | Examples |
|---|---|---|
| **High** | Tongue is raised near the roof of the mouth | `i`, `u`, `ɨ`, `ɯ` |
| **Near-high** | Slightly lowered from high | `ɪ`, `ʊ` |
| **High-mid** | Tongue raised above mid | `e`, `o`, `ø`, `ɤ` |
| **Mid** | Tongue at middle height | `e̞`, `ə`, `o̞` |
| **Low-mid** | Tongue below mid | `ɛ`, `ɔ`, `œ`, `ʌ` |
| **Near-low** | Slightly raised from low | `æ`, `ɐ` |
| **Low** | Tongue is at its lowest position | `a`, `ɑ`, `ɶ`, `ɒ` |

### Vowel Backness

How far back the tongue body is positioned:

| Backness | Description | Examples |
|---|---|---|
| **Front** | Tongue pushed forward | `i`, `e`, `ɛ`, `a` |
| **Near-front** | Slightly behind front | `ɪ`, `ʏ` |
| **Central** | Tongue in the center | `ɨ`, `ə`, `ɜ`, `ɐ` |
| **Near-back** | Slightly in front of back | `ʊ` |
| **Back** | Tongue pulled to the back | `u`, `o`, `ɔ`, `ɑ` |

### Lip Rounding

- **Unrounded**: Lips are spread or neutral (e.g., `i`, `e`, `ɛ`, `a`, `ɨ`, `ɯ`)
- **Rounded**: Lips are rounded (e.g., `y`, `ø`, `œ`, `ɶ`, `u`, `o`, `ɔ`, `ɒ`)

In the vowel chart, unrounded vowels appear on the **left** (white background) and rounded vowels on the **right** (gray background) of each cell.

---

### Vowel Chart

|  | Front | Near-front | Central | Near-back | Back |
|---|---|---|---|---|---|
| **High** | i  y | ï  ÿ | ɨ  ʉ | ɯ̈  ü | ɯ  u |
| **Near-high** | i̞  y̞ | ɪ  ʏ | ɪ̈  ʊ̈ | ɯ̽  ʊ | ɯ̞  u̞ |
| **High-mid** | e  ø | ë  ø̈ | ɘ  ɵ | ɤ̈  ö | ɤ  o |
| **Mid** | e̞  ø̞ | ë̞  ø̞̈ | ə  ɵ̞ | ɤ̞̈  ö̞ | ɤ̞  o̞ |
| **Low-mid** | ɛ  œ | ɛ̈  œ̈ | ɜ  ɞ | ʌ̈  ɔ̈ | ʌ  ɔ |
| **Near-low** | æ  œ̞ | æ̈  ɶ̽ | ɐ  ɞ̞ | ɑ̽  ɒ̽ | ʌ̞  ɔ̞ |
| **Low** | a  ɶ | ä  ɶ̈ | ɐ̞  ɐ̞̹ | ɑ̈  ɒ̈ | ɑ  ɒ |

In each cell, **left = unrounded**, **right = rounded**.

---

## Nasal Vowels

Nasal vowels are produced with the velum (soft palate) lowered, allowing air to pass through the nasal cavity simultaneously. They are notated by adding a tilde `̃` over the vowel symbol:

| Oral | Nasal |
|---|---|
| `a` | `ã` |
| `e` | `ẽ` |
| `i` | `ĩ` |
| `o` | `õ` |
| `u` | `ũ` |
| `ɛ` | `ɛ̃` |
| `ɔ` | `ɔ̃` |
| `ə` | `ə̃` |

Nasal vowels are common in French (e.g., *vin* `/vɛ̃/`), Portuguese, and many other languages. The full nasal vowel chart mirrors the oral vowel chart, with every oral vowel having a nasal counterpart.

---

## Rarer Consonant Manners

### Lateral Fricatives and Affricates

Lateral fricatives combine lateral airflow with friction:

| Symbol | Description |
|---|---|
| `ɬ` | Voiceless alveolar lateral fricative (Welsh *ll* in *Llan*) |
| `ɮ` | Voiced alveolar lateral fricative |
| `tɬ` | Voiceless alveolar lateral affricate |
| `dɮ` | Voiced alveolar lateral affricate |
| `ʎ̝̥` | Voiceless palatal lateral fricative |
| `ʟ̝̥` | Voiceless velar lateral fricative |

---

### Implosive Stops

Implosives use a downward movement of the larynx to create suction, resulting in an inward (ingressive) airstream at the glottis while the mouth still releases:

| Symbol | Place |
|---|---|
| `ɓ` | Voiced bilabial implosive |
| `ɗ` | Voiced alveolar implosive |
| `ʄ` | Voiced palatal implosive |
| `ɠ` | Voiced velar implosive |
| `ʛ` | Voiced uvular implosive |

Voiceless implosives are also possible (e.g., `ɓ̥`).

---

### Ejective Sounds

Ejectives use a closed glottis with an upward larynx movement, creating an egressive glottalic airstream. They are marked with the apostrophe-like symbol `ʼ`:

| Type | Examples |
|---|---|
| **Ejective stops** | `pʼ`, `tʼ`, `kʼ`, `qʼ` |
| **Ejective affricates** | `tsʼ`, `tʃʼ`, `kxʼ`, `qχʼ` |
| **Ejective lateral affricates** | `tɬʼ`, `kʟ̝̥ʼ` |
| **Ejective fricatives** | `sʼ`, `ʃʼ`, `xʼ`, `χʼ` |

Ejectives are common in languages such as Georgian, Hausa, and many indigenous languages of the Americas.

---

## Click Consonants

Clicks are produced with two simultaneous closures in the mouth; the release of the front closure creates the click sound. They use a lingual ingressive airstream and are common in Khoisan languages and some Bantu languages (e.g., Zulu, Xhosa).

### Click Places (Front Closure)

| Symbol | Place | Description |
|---|---|---|
| `ʘ` | Bilabial | Lips |
| `ǀ` | Dental | Tongue tip + upper teeth |
| `ǁ̪` | Lateral dental | Lateral tongue + teeth |
| `ǃ` | Alveolar | Tongue tip + alveolar ridge |
| `ǃ¡` | Slapped alveolar | Tongue slap against alveolar ridge |
| `ǁ` | Lateral alveolar | Lateral tongue + alveolar ridge |
| `ǃ˞` | Retroflex | Tongue tip curled back |
| `ǂ` | Palatal | Tongue body + hard palate |

### Click Back Closures and Modifications

The back closure (and its release) determines the secondary properties of a click:

| Modifier | Meaning |
|---|---|
| *(none)* | Voiceless velar stop back closure |
| `q` suffix | Uvular back closure (e.g., `ǃq`) |
| `ʰ` suffix | Aspirated (e.g., `ǃʰ`) |
| `̬` diacritic | Voiced (e.g., `ǃ̬`) |
| `ʼ` suffix | Ejective (e.g., `ǃʼ`) |
| `ɴ` / `̃` | Nasal (e.g., `ǃɴ`, `ǃ̃`) |
| `x` suffix | Velar affricate release (e.g., `ǃx`) |
| `χ` suffix | Uvular affricate release (e.g., `ǃqχ`) |

---

## Diacritics and Modifiers

Diacritics are small marks added to base IPA symbols to modify their phonetic value:

| Diacritic | Notation | Meaning |
|---|---|---|
| Aspirated | `ʰ` (after) | Followed by a puff of air: `pʰ`, `tʰ`, `kʰ` |
| Breathy voiced | `ʱ` (after) | Voiced with breathy quality: `bʱ` |
| Voiceless | `̥` (under) | Devoiced variant: `n̥`, `l̥` |
| Voiced | `̬` (under) | Voiced variant: `s̬` |
| Nasalized | `̃` (over) | Nasalized sound: `ã`, `ẽ` |
| Lateral | `l` (after) | Lateral release or coarticulation |
| Labialized | `ʷ` (after) | Lip rounding added: `kʷ`, `gʷ` |
| Palatalized | `ʲ` (after) | Palatal secondary articulation: `tʲ` |
| Retroflexed | `˞` (after) | Retroflexed: `ǃ˞` |
| Raised | `̝` (under) | Raised articulation: `r̝` (fricative trill) |
| Lowered | `̞` (under) | Lowered articulation: `β̞` (approximant) |
| Advanced | `̟` (under) | Articulation moved forward |
| Retracted | `̠` (under) | Articulation moved back: `n̠`, `t̠` |
| Ejective | `ʼ` (after) | Ejective airstream: `pʼ`, `kʼ` |
| Dental | `̪` (under) | Dental place: `t̪`, `d̪`, `n̪` |
| Linguolabial | `̼` (under) | Linguolabial place: `t̼`, `n̼` |
| Low uvular | `ʌ` (after) | Low uvular articulation: `qʌ`, `ɢʌ` |

---

## Tips for Use

1. **Start with the most common sounds.** The charts are ordered roughly from most common to most obscure: common consonant manners → vowels → nasal vowels → rarer consonant manners → clicks.
2. **For consonants**, identify the place first (column) then the manner (row), then check whether the sound is voiced or voiceless.
3. **For vowels**, identify the height (row), then backness (column), then rounding (left = unrounded, right = rounded).
4. **Use diacritics** to fine-tune representations. For example, `t̪` specifies a dental (not alveolar) `t`.
5. **Verify spellings** against multiple authoritative sources (e.g., the official IPA chart from the International Phonetic Association) — some representations can be non-canonical.
6. **Not all symbol combinations represent real attested sounds.** The IPA covers theoretical articulatory space; some sounds may not appear in any known language.
7. **Coarticulated sounds** (e.g., Arabic emphatic consonants) are generally represented by combining base symbols with diacritics, though this can become complex.

---

*Source: [IPA Charts with Audio — jbdowse.com/ipa](https://jbdowse.com/ipa/) | International Phonetic Association*
