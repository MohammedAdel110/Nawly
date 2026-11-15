---
name: "Nawly Web Design System"
description: "Arabic-first, high-clarity desktop design system for Nawly hyper-local marketplace in Nawa, Qalyubia."
---

## Overview

Nawly is a hyper-local ordering and delivery marketplace built specifically for the village of Nawa in Qalyubia, Egypt, bridging rural local merchants (restaurants, bakeries, groceries, pharmacies), neighborhood couriers, and residents where national platforms do not operate.

The desktop web design language embodies "Rural Utility with Urban Precision": trustworthy, warm Egyptian village warmth grounded in earth and agricultural greens, high-contrast visual cues for low-bandwidth networks, and unapologetic Arabic-first (RTL) ergonomics. Every screen highlights exact cash values in EGP, clear order progress, forgiving inputs with accessible hit targets, and honest, welcoming Egyptian communal tone.

## Colors

The color palette centers on **Nile Teal** (Primary: `#006874`), a dependable, refreshing Egyptian green-blue that symbolizes stability, commerce, and agricultural prosperity. It is supported by **Citrus Gold** (Accent: `#B45309` / Amber-700) for warnings and delivery alerts, and **Date Palm Green** (Success: `#15803D`) for confirmed orders and cash collections. Neutral backgrounds use soft sand-tinted warm surfaces (`#F8F9FA` to `#FFFFFF`) paired with high-contrast obsidian slate (`#191C1D`) for effortless legibility under bright sunlight and older monitors.

- **Primary (`#006874`):** Anchors primary actions, brand marks, and active states.
- **On Primary (`#FFFFFF`):** Maximum contrast against Nile Teal.
- **Secondary (`#4A6267`):** Neutral supportive containers, secondary tabs, and metadata labels.
- **Surface (`#FBFDFD` / `#F1F5F6`):** Clean, crisp desktop background surfaces with gentle separation.
- **Currency & Cash Accent (`#047857`):** Explicitly communicates money, delivery fees, and minimum orders in Egyptian Pounds (ج.م / EGP).

```yaml
colors:
  accent: "#B45309"
  success: "#15803D"
  error: "#BA1A1A"
  warning: "#D97706"
  light:
    primary: "#006874"
    on-primary: "#FFFFFF"
    primary-container: "#97F0FF"
    on-primary-container: "#001F24"
    secondary: "#4A6267"
    on-secondary: "#FFFFFF"
    secondary-container: "#CCE8ED"
    on-secondary-container: "#051F23"
    surface: "#FBFDFD"
    on-surface: "#191C1D"
    surface-variant: "#DBE4E6"
    on-surface-variant: "#3F484A"
    outline: "#6F797B"
    outline-variant: "#BFC8CA"
  dark:
    primary: "#4FD8EB"
    on-primary: "#00363D"
    primary-container: "#004F58"
    on-primary-container: "#97F0FF"
    secondary: "#B0CBD0"
    on-secondary: "#1B3439"
    secondary-container: "#324B4F"
    on-secondary-container: "#CCE8ED"
    surface: "#191C1D"
    on-surface: "#E1E3E3"
    surface-variant: "#3F484A"
    on-surface-variant: "#BFC8CA"
    outline: "#899294"
    outline-variant: "#3F484A"
```

## Typography

Nawly is designed strictly Arabic-first (`dir="rtl"`) using **Cairo** as the unified headline and display typeface—renowned for its clean geometric terminals and superb readability at both massive billboard sizes and desktop headers. **Tajawal** serves as the body typeface, providing fluid, natural naskh proportions that read effortlessly in Arabic paragraphs, input labels, and legal clauses. **Source Code Pro** handles tabular numerals, phone numbers (`+20`), order references, and 6-digit OTP codes.

```yaml
typography:
  display-lg:
    fontFamily: "Cairo"
    fontSize: 40px
    fontWeight: 700
    lineHeight: 48px
    letterSpacing: -0.5px
  display-lg-mobile:
    fontFamily: "Cairo"
    fontSize: 28px
    fontWeight: 700
    lineHeight: 36px
  headline-xl:
    fontFamily: "Cairo"
    fontSize: 34px
    fontWeight: 700
    lineHeight: 42px
  headline-xl-mobile:
    fontFamily: "Cairo"
    fontSize: 26px
    fontWeight: 700
    lineHeight: 34px
  headline-lg:
    fontFamily: "Cairo"
    fontSize: 28px
    fontWeight: 600
    lineHeight: 36px
  headline-lg-mobile:
    fontFamily: "Cairo"
    fontSize: 22px
    fontWeight: 600
    lineHeight: 30px
  headline-md:
    fontFamily: "Cairo"
    fontSize: 22px
    fontWeight: 600
    lineHeight: 30px
  headline-sm:
    fontFamily: "Cairo"
    fontSize: 18px
    fontWeight: 600
    lineHeight: 26px
  body-lg:
    fontFamily: "Tajawal"
    fontSize: 18px
    fontWeight: 400
    lineHeight: 28px
  body-md:
    fontFamily: "Tajawal"
    fontSize: 16px
    fontWeight: 400
    lineHeight: 24px
  body-sm:
    fontFamily: "Tajawal"
    fontSize: 14px
    fontWeight: 400
    lineHeight: 20px
  label-lg:
    fontFamily: "Cairo"
    fontSize: 16px
    fontWeight: 600
    lineHeight: 24px
  label-md:
    fontFamily: "Cairo"
    fontSize: 14px
    fontWeight: 600
    lineHeight: 20px
  label-sm:
    fontFamily: "Cairo"
    fontSize: 12px
    fontWeight: 500
    lineHeight: 16px
  code-md:
    fontFamily: "Source Code Pro"
    fontSize: 16px
    fontWeight: 600
    lineHeight: 22px
```

## Layout & Spacing

Desktop web layouts target a comfortable 1440px widescreen canvas with a responsive 12-column grid and standard max-width containers (`max-w-7xl` / 1280px or `max-w-5xl` / 1024px for multi-step onboarding). Split-screen authentication layouts use a 5:7 or 1:1 balance, anchoring brand storytelling and village context on the right (in RTL) and active authentication forms on the left.

The spacing scale is built on an 8px base rhythm with 4px half-steps for compact form groups, phone prefix badges, and OTP input boxes.

```yaml
spacing:
  gutter-xs: 4px
  gutter-sm: 8px
  gutter-md: 16px
  gutter-lg: 24px
  gutter-xl: 32px
  section: 48px
  page-margin: 32px
```

## Elevation & Depth

To preserve performance on lower-power devices and low-bandwidth connections, Nawly avoids heavy backdrop blurs or deep stacking. Elevation uses crisp, subtle 1px border outlines (`border border-light-outline-variant/60`) complemented by warm ambient micro-shadows (`0 4px 20px -2px rgba(25, 28, 29, 0.05)`). Focused fields and active radio cards gain a defined 2px primary ring (`ring-2 ring-light-primary`).

## Shapes

Corners feature confident, welcoming radii: 12px for standard cards and form inputs, 16px for prominent onboarding cards and modal dialogs, and 9999px pill shapes for phone prefix tags, status badges, and OTP indicators.

```yaml
rounded:
  sm: 6px
  DEFAULT: 10px
  md: 12px
  lg: 16px
  xl: 20px
  full: 9999px
```

## Motion & Micro-interaction

Micro-interactions are snappy and informative. Transitions emphasize immediate validation feedback on Egyptian mobile numbers (11 digits), OTP box auto-advancement, and tab switches without layout reflows.

```yaml
motion:
  state-change:
    duration: 120ms
    delay: 0ms
    timingFunction: ease-out
  surface-enter:
    duration: 220ms
    delay: 0ms
    timingFunction: cubic-bezier(0.16, 1, 0.3, 1)
  surface-exit:
    duration: 140ms
    delay: 0ms
    timingFunction: cubic-bezier(0.3, 0, 1, 1)
```

## Voice & Content Strategy

- **Authentic Egyptian Arabic (لهجة مألوفة ولغة عربية فصحى مبسطة):** Microcopy speaks directly to local Nawa store owners, neighborhood couriers (كباتن التوصيل), and families.
- **Clarity of Money (وضوح الأسعار والمبالغ):** All fees, minimum order values, and commission notes are explicitly labeled in Egyptian Pounds (ج.م) with zero hidden fees.
- **Unambiguous Primary Actions:** Clear verb phrases like "إرسال رمز التحقق" (Send verification code), "تأكيد الدخول" (Confirm sign-in), "إرسال طلب الانضمام" (Submit application).
- **Forgiving & Reassuring:** Provides instant links to edit mistakes ("تغيير رقم الهاتف", "إعادة إرسال الرمز"), progress indicators, and WhatsApp instant support for merchants and couriers.

## Components

Component implementations live as individual HTML sticker sheet files in
`components/`. Each file contains the canonical, styled HTML components with
`<!-- COMPONENT: name -->` comment markers. Read each file before use.

- **Badge** → `@components/badge.html`: `badge-numeric`, `badge-dot`
- **Button** → `@components/button.html`: `button-primary`, `button-icon`, `button-with-icon`
- **Card** → `@components/card.html`: `card-elevated`, `card-action`, `card-interactive`, `card-interactive-2`
- **Input Field** → `@components/input-field.html`: `input-field-text`, `input-field-icon`
