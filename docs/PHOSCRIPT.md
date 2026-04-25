# PhoScript 1.0 Specification

> A text-based markup language for millisecond-accurate prosodic representation of spoken utterances.

See also:
- [IPA_REFERENCE.md](IPA_REFERENCE.md) — full IPA symbol reference for values used in `ipa` attributes
- [API.md](API.md) — `PhoScriptWriter` helper and related service APIs
- [ARCHITECTURE.md](ARCHITECTURE.md) — where PhoScript fits in the application output pipeline
- [AGENTS.md](AGENTS.md) — coding guidelines for contributors extending the format

---

## Table of Contents

1. [Design Goals](#1-design-goals)
2. [Terminology](#2-terminology)
3. [File Format](#3-file-format)
4. [Top-Level Structure](#4-top-level-structure)
5. [The `<word>` Block](#5-the-word-block)
6. [The `<phon>` Atom](#6-the-phon-atom)
   - [6.1 Timestamp Attributes](#61-timestamp-attributes)
   - [6.2 Pitch (F0) Attributes](#62-pitch-f0-attributes)
   - [6.3 Intensity Attributes](#63-intensity-emphasis-energy)
   - [6.4 Contour Enumeration](#64-contour-enumeration)
   - [6.5 Voice Quality](#65-voice-quality)
   - [6.6 Coarticulation Markers](#66-coarticulation-markers)
7. [Pauses](#7-pauses)
8. [Prosody Spans](#8-prosody-spans)
9. [Encoding Conventions](#9-encoding-conventions)
10. [Complete Worked Example](#10-complete-worked-example)
11. [Extension Points](#11-extension-points)

---

## 1. Design Goals

PhoScript encodes a spoken sentence with enough fidelity that a speech synthesizer, linguist, or reproduction system can reconstruct the prosody of the original — not just the words, but how they were said. Goals in priority order:

1. Millisecond-accurate timestamps for every acoustic segment
2. Full prosodic metadata: pitch (F0), energy, voice quality, contour shape
3. Pause representation at both inter-word and intra-word levels
4. Speaker-relative encoding so values are portable across voice types
5. Human-readable plain text with deterministic parsing
6. Lossless round-trip: parse → serialize → parse yields identical structure

---

## 2. Terminology

> **A note on the atomic unit:** The spec deliberately avoids "grapheme" (a written character) and "phoneme" (an abstract mental category). The atom here is a **phon** — a single, bounded acoustic phone as it was realized in this utterance, represented in IPA. A phon is what was actually said, not what the language abstractly requires.

| Term | Definition |
|---|---|
| **phon** | A single realized phone (IPA), with timestamps and acoustic metadata |
| **word** | An orthographic word grouping one or more phons |
| **pause** | A silence or breath event, with type and duration |
| **prosody span** | A suprasegmental arc spanning multiple phons or words |
| **f0** | Fundamental frequency in Hz — the measurable pitch of the voice |
| **f0_rel** | F0 expressed in semitones relative to the speaker baseline |
| **contour** | The shape of F0 movement within a phon or span |
| **coarticulation** | Acoustic blending between adjacent phons |

---

## 3. File Format

| Property | Value |
|---|---|
| Extension | `.phos` |
| Encoding | UTF-8, no BOM |
| Line endings | LF |
| Structure | Indented tag-based markup with named attributes |
| Comments | Lines beginning with `##` are ignored by parsers |

Syntax uses angle-bracket tags with named attributes. Multi-line blocks use indented children. Attribute values containing spaces must be quoted. IPA content is always enclosed in `/` delimiters following linguistic convention.

---

## 4. Top-Level Structure

Every PhoScript document is a single `<sentence>` block:

```xml
<sentence id="utt_001" lang="en-US" duration_ms="2840">

  <meta recording_id="rec_20240312_001" date="2024-03-12"
        sample_rate="44100" bit_depth="24"/>

  <speaker id="spk_A" gender="F"
           f0_mean_hz="196" f0_range_hz="82-420"
           f0_p10_hz="130" f0_p90_hz="310"
           intensity_mean_db="68" intensity_range_db="54-82"
           rate_sps="4.2" voice_quality="modal"/>

  ## Utterance content follows
  ...

</sentence>
```

The `<speaker>` block is the normalization anchor for all relative values.

| Attribute | Description |
|---|---|
| `f0_mean_hz` | Mean fundamental frequency for this speaker |
| `f0_p10_hz` / `f0_p90_hz` | 10th and 90th percentile F0, establishing the working range |
| `intensity_mean_db` | Mean RMS energy level in dBFS |
| `rate_sps` | Speaking rate in syllables per second |
| `voice_quality` | Default phonation type for this speaker |

`f0_rel` values in `<phon>` atoms are semitones from `f0_mean_hz`.

---

## 5. The `<word>` Block

Words are the primary organizational unit above the phon level:

```xml
<word orth="really" t_start="0" t_end="620"
      stress_pattern="10" syllables="2"
      phrase_boundary="IP_end"
      prosody_fn="incredulous-question">

  ... phons and pauses ...

</word>
```

| Attribute | Type | Description |
|---|---|---|
| `orth` | string | Orthographic form |
| `t_start` / `t_end` | integer ms | Word boundaries |
| `stress_pattern` | string | `1` = primary stress, `0` = unstressed, per syllable left-to-right |
| `phrase_boundary` | enum | `ip_end` (minor phrase), `IP_end` (intonation phrase), `utterance_end`, `none` |
| `prosody_fn` | string | Pragmatic/discourse function label — free-form annotation |

---

## 6. The `<phon>` Atom

The phon is the irreducible unit of PhoScript. Every acoustic segment maps to exactly one phon. Phons within a word are listed in temporal order.

```xml
<phon ipa="/r/"
      t_start="0" t_end="68" dur_ms="68"
      f0_hz="210" f0_rel_st="+1.2"
      f0_contour="rise"
      intensity_db="74" intensity_rel="+6"
      voice_quality="modal"
      coart_lead="none" coart_lag="rhotic-coloring"/>
```

### 6.1 Timestamp Attributes

| Attribute | Type | Description |
|---|---|---|
| `t_start` | integer ms | Onset of this phone, from utterance start |
| `t_end` | integer ms | Offset of this phone |
| `dur_ms` | integer ms | Duration (must equal `t_end − t_start`) |

Timestamps are absolute from utterance start (t=0). Adjacent phons must be contiguous or separated by a `<pause>` element.

### 6.2 Pitch (F0) Attributes

| Attribute | Type | Description |
|---|---|---|
| `f0_hz` | float | Instantaneous F0 in Hz at the phon's midpoint |
| `f0_rel_st` | float | Semitones above (+) or below (−) speaker `f0_mean_hz` |
| `f0_onset_hz` | float | F0 at phon onset *(optional; for contour precision)* |
| `f0_offset_hz` | float | F0 at phon offset *(optional)* |
| `f0_contour` | enum | Shape of F0 movement — see §6.4 |
| `f0_rate_st_per_ms` | float | Rate of F0 change in semitones/ms *(for steep glides)* |

For voiceless phones, all F0 attributes are omitted and an implicit `f0_hz="0"` is assumed.

### 6.3 Intensity (Emphasis / Energy)

| Attribute | Type | Description |
|---|---|---|
| `intensity_db` | float | RMS energy level in dBFS |
| `intensity_rel` | float | dB above (+) or below (−) speaker `intensity_mean_db` |

`intensity_rel` is the portable value; `intensity_db` is the recording-specific absolute.

### 6.4 Contour Enumeration

The `f0_contour` attribute describes the pitch movement shape within the phon's duration:

| Value | Description |
|---|---|
| `level` | Flat; F0 stable throughout |
| `rise` | Monotonic increase |
| `fall` | Monotonic decrease |
| `rise-fall` | Peak in the middle |
| `fall-rise` | Valley in the middle |
| `convex` | Smooth arch (gradual rise then fall) |
| `concave` | Smooth scoop (gradual fall then rise) |
| `step-up` | Abrupt rise at onset |
| `step-down` | Abrupt fall at onset |
| `creaky` | F0 irregular due to creaky phonation |
| `na` | Voiceless; no F0 |

### 6.5 Voice Quality

| Value | Description |
|---|---|
| `modal` | Normal phonation |
| `breathy` | Increased breathiness (glottal frication) |
| `creaky` | Laryngealization / vocal fry |
| `falsetto` | High register, thin contact |
| `strained` | Hyperadducted; pressed phonation |
| `whisper` | Voiceless, turbulent airflow |

### 6.6 Coarticulation Markers

Coarticulation captures the acoustic influence of neighboring phones — essential for accurate reproduction of natural speech.

| Attribute | Example values | Description |
|---|---|---|
| `coart_lead` | `nasalized`, `rounded`, `palatalized`, `pharyngealized`, `none` | Anticipatory influence from the following phone |
| `coart_lag` | `rhotic-coloring`, `nasalized`, `rounded`, `devoiced`, `lateral-release`, `none` | Carryover influence from the preceding phone |

---

## 7. Pauses

PhoScript distinguishes six pause types, usable both between and within words:

```xml
<pause t_start="620" t_end="680" dur_ms="60"
       type="micro" breath="false"/>

<pause t_start="1200" t_end="1520" dur_ms="320"
       type="lexical" breath="true" breath_intensity_rel="-8"/>
```

| `type` | Duration range | Description |
|---|---|---|
| `micro` | < 80 ms | Sub-perceptual gap; often a coarticulation boundary |
| `short` | 80–200 ms | Word boundary pause, no breath |
| `lexical` | 200–600 ms | Deliberate phrasing pause |
| `long` | > 600 ms | Planning pause, dramatic effect, structural break |
| `hesitation` | any | Filled pause position (use `<phon ipa="/ə.../">` for the filled sound itself) |
| `glottal_stop` | < 50 ms | Hard onset before a vowel; full closure, not silence |

When `breath="true"`, include `breath_intensity_rel` (dB relative to speaker mean) and optionally `breath_dur_ms`.

---

## 8. Prosody Spans

Where a prosodic feature — a tonal movement, an emphasis arc, a register shift — spans multiple phons or words, a `<prosody_span>` element provides the suprasegmental description. This is separate from per-phon F0 values; it captures the intentional arc.

```xml
<prosody_span t_start="0" t_end="620"
              type="nuclear_tone"
              contour="rise"
              f0_start_hz="196" f0_end_hz="380"
              f0_start_rel_st="0" f0_end_rel_st="+11.4"
              register="high"
              emphasis="strong"
              intonation_fn="WH-question"
              rate_change="none"/>
```

| Attribute | Description |
|---|---|
| `type` | `nuclear_tone`, `pre-head`, `head`, `tail`, `emphasis_arc`, `rhythm_group` |
| `contour` | Same enum as phon-level (the overall shape of the span) |
| `f0_start_hz` / `f0_end_hz` | Span boundary F0 values in Hz |
| `f0_start_rel_st` / `f0_end_rel_st` | Span boundary F0 in semitones from speaker mean |
| `register` | `extra-low`, `low`, `mid`, `high`, `extra-high` |
| `emphasis` | `none`, `light`, `strong`, `contrastive` |
| `intonation_fn` | `statement`, `yes-no-question`, `WH-question`, `exclamation`, `continuation`, `list-item`, `incredulous`, `sarcastic`, `emphatic` |
| `rate_change` | `none`, `accelerando`, `ritardando`, `held` |

---

## 9. Encoding Conventions

**Semitone calculation:**

```
f0_rel_st = 12 × log₂(f0_hz / f0_mean_hz)
```

**Timestamp precision:** Integers in milliseconds. Sub-millisecond precision is not required; measurement error in forced alignment is typically ±5–10 ms.

**IPA convention:** Phone strings in `ipa` attributes use slash-delimited IPA. Narrow transcription is optional; broad transcription is acceptable. Diacritics and modifiers follow the base symbol inside the slashes: `/pʰ/`, `/ɾ̃/`, `/r̥/`.

**Attribute ordering (non-normative):** `ipa`, then timestamps, then F0, then intensity, then contour, then voice quality, then coarticulation. Parsers must not depend on order.

---

## 10. Complete Worked Example

Sentence: **"Really?"** — spoken with incredulous rising intonation.

```xml
## PhoScript 1.0
## "Really?" – incredulous rising question
## Speaker: Adult female, American English

<sentence id="utt_demo_001" lang="en-US" duration_ms="700">

  <meta recording_id="demo" date="2025-10-28" sample_rate="44100"/>

  <speaker id="spk_A" gender="F"
           f0_mean_hz="196" f0_range_hz="82-420"
           f0_p10_hz="130" f0_p90_hz="310"
           intensity_mean_db="68"
           rate_sps="4.2"
           voice_quality="modal"/>

  <prosody_span t_start="0" t_end="700"
                type="nuclear_tone"
                contour="rise"
                f0_start_hz="175" f0_end_hz="388"
                f0_start_rel_st="-2.0" f0_end_rel_st="+12.8"
                register="high"
                emphasis="contrastive"
                intonation_fn="incredulous-question"
                rate_change="none"/>

  <word orth="Really" t_start="0" t_end="620"
        stress_pattern="10" syllables="2"
        phrase_boundary="IP_end"
        prosody_fn="incredulous-question">

    ## Syllable 1: "Ree" /ˈriː/

    <phon ipa="/r/"
          t_start="0" t_end="68" dur_ms="68"
          f0_hz="175" f0_rel_st="-2.0"
          f0_onset_hz="172" f0_offset_hz="182"
          f0_contour="rise"
          intensity_db="71" intensity_rel="+3"
          voice_quality="modal"
          coart_lead="none" coart_lag="none"/>

    <phon ipa="/iː/"
          t_start="68" t_end="240" dur_ms="172"
          f0_hz="220" f0_rel_st="+2.0"
          f0_onset_hz="182" f0_offset_hz="262"
          f0_contour="rise"
          f0_rate_st_per_ms="0.04"
          intensity_db="76" intensity_rel="+8"
          voice_quality="modal"
          coart_lead="none" coart_lag="none"/>

    ## Syllable boundary micro-pause
    <pause t_start="240" t_end="268" dur_ms="28"
           type="micro" breath="false"/>

    ## Syllable 2: "lee" /li/

    <phon ipa="/l/"
          t_start="268" t_end="340" dur_ms="72"
          f0_hz="280" f0_rel_st="+6.2"
          f0_onset_hz="262" f0_offset_hz="295"
          f0_contour="rise"
          intensity_db="72" intensity_rel="+4"
          voice_quality="modal"
          coart_lead="none" coart_lag="lateral-release"/>

    <phon ipa="/i/"
          t_start="340" t_end="540" dur_ms="200"
          f0_hz="340" f0_rel_st="+9.5"
          f0_onset_hz="295" f0_offset_hz="388"
          f0_contour="rise"
          f0_rate_st_per_ms="0.032"
          intensity_db="74" intensity_rel="+6"
          voice_quality="breathy"
          coart_lead="none" coart_lag="none"/>

    ## Final /i/ lengthening — breathiness encodes incredulity

    <phon ipa="/i̤/"
          t_start="540" t_end="620" dur_ms="80"
          f0_hz="388" f0_rel_st="+12.8"
          f0_onset_hz="388" f0_offset_hz="380"
          f0_contour="fall-rise"
          f0_rate_st_per_ms="-0.01"
          intensity_db="70" intensity_rel="+2"
          voice_quality="breathy"
          coart_lead="none" coart_lag="none"/>

  </word>

  ## Sentence-final silence
  <pause t_start="620" t_end="700" dur_ms="80"
         type="short" breath="false"/>

</sentence>
```

The prosodic arc is encoded at three levels simultaneously: the `<prosody_span>` captures the intentional nuclear rise from −2.0 to +12.8 semitones across the whole word; each `<phon>` records the instantaneous F0 at its midpoint and the local contour shape; and the voice quality shift from `modal` to `breathy` in the second syllable encodes the paralinguistic texture of disbelief. The micro-pause at 240 ms marks the syllable boundary reduction without falsely implying a word boundary.

---

## 11. Extension Points

The spec reserves three attribute namespaces for domain-specific extensions without breaking core parsers:

| Namespace | Purpose |
|---|---|
| `x_*` | Experimental, non-validated attributes |
| `asr_*` | Metadata from automated forced-alignment pipelines (confidence scores, model ID) |
| `tts_*` | Hints for synthesis targets (synthesizer ID, voice profile) |

**Example:**

```xml
<phon ipa="/r/" t_start="0" t_end="68" dur_ms="68"
      asr_confidence="0.94" asr_model="wav2vec2-large-xlsr"
      ... />
```

---

*PhoScript 1.0 — specification document*