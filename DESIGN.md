# DESIGN.md

> 蒸餾來源：[Tailwind CSS UI (Community)](https://www.figma.com/design/3EAJ432g5izaE7jKcIFDFu/Tailwind-CSS-UI--Community-) Figma 社群範本
> 因團隊目前沒有自己的設計稿，先以這份社群範本作為統一視覺標準的起點，之後設計師可在 Figma 上調整、再重新蒸餾更新本檔。

## Color Styles

**gray**

| Token | Hex |
|---|---|
| gray/100 | `#F4F4F5` |
| gray/200 | `#E4E4E7` |
| gray/300 | `#D4D4D8` |
| gray/400 | `#A1A1AA` |
| gray/500 | `#71717A` |
| gray/600 | `#52525B` |
| gray/700 | `#3F3F46` |
| gray/800 | `#27272A` |
| gray/900 | `#18181B` |

**coolGray**

| Token | Hex |
|---|---|
| coolGray/50 | `#F9FAFB` |
| coolGray/100 | `#F3F4F6` |
| coolGray/200 | `#E5E7EB` |
| coolGray/300 | `#D1D5DB` |
| coolGray/400 | `#9CA3AF` |
| coolGray/500 | `#6B7280` |
| coolGray/600 | `#4B5563` |
| coolGray/700 | `#374151` |
| coolGray/800 | `#1F2937` |
| coolGray/900 | `#111827` |

**red**

| Token | Hex |
|---|---|
| red/50 | `#FEF2F2` |
| red/100 | `#FEE2E2` |
| red/200 | `#FECACA` |
| red/300 | `#FCA5A5` |
| red/400 | `#F87171` |
| red/500 | `#EF4444` |
| red/600 | `#DC2626` |
| red/700 | `#B91C1C` |
| red/800 | `#991B1B` |
| red/900 | `#7F1D1D` |

**orange**

| Token | Hex |
|---|---|
| orange/100 | `#FFEDD5` |
| orange/500 | `#F97316` |
| orange/700 | `#C2410C` |

**yellow**

| Token | Hex |
|---|---|
| yellow/50 | `#FEFCE8` |
| yellow/100 | `#FEF9C3` |
| yellow/200 | `#FEF08A` |
| yellow/300 | `#FDE047` |
| yellow/400 | `#FACC15` |
| yellow/500 | `#EAB308` |
| yellow/600 | `#CA8A04` |
| yellow/700 | `#A16207` |
| yellow/800 | `#854D0E` |
| yellow/900 | `#713F12` |

**green**

| Token | Hex |
|---|---|
| green/50 | `#F0FDF4` |
| green/100 | `#DCFCE7` |
| green/200 | `#BBF7D0` |
| green/300 | `#86EFAC` |
| green/400 | `#4ADE80` |
| green/500 | `#22C55E` |
| green/600 | `#16A34A` |
| green/700 | `#15803D` |
| green/800 | `#166534` |
| green/900 | `#14532D` |

**teal**

| Token | Hex |
|---|---|
| teal/100 | `#CCFBF1` |
| teal/200 | `#99F6E4` |
| teal/400 | `#2DD4BF` |
| teal/500 | `#14B8A6` |
| teal/900 | `#134E4A` |

**blue**

| Token | Hex |
|---|---|
| blue/50 | `#EFF6FF` |
| blue/100 | `#DBEAFE` |
| blue/200 | `#BFDBFE` |
| blue/300 | `#93C5FD` |
| blue/400 | `#60A5FA` |
| blue/500 | `#3B82F6` |
| blue/600 | `#2563EB` |
| blue/700 | `#1D4ED8` |
| blue/800 | `#1E40AF` |
| blue/900 | `#1E3A8A` |

**indigo**

| Token | Hex |
|---|---|
| indigo/50 | `#EEF2FF` |
| indigo/100 | `#E0E7FF` |
| indigo/200 | `#C7D2FE` |
| indigo/300 | `#A5B4FC` |
| indigo/400 | `#818CF8` |
| indigo/500 | `#6366F1` |
| indigo/600 | `#4F46E5` |
| indigo/700 | `#4338CA` |
| indigo/800 | `#3730A3` |
| indigo/900 | `#312E81` |

**purple**

| Token | Hex |
|---|---|
| purple/50 | `#FAF5FF` |
| purple/100 | `#F3E8FF` |
| purple/200 | `#E9D5FF` |
| purple/300 | `#D8B4FE` |
| purple/400 | `#C084FC` |
| purple/500 | `#A855F7` |
| purple/600 | `#9333EA` |
| purple/700 | `#7E22CE` |
| purple/800 | `#6B21A8` |
| purple/900 | `#581C87` |

**pink**

| Token | Hex |
|---|---|
| pink/50 | `#FDF2F8` |
| pink/100 | `#FCE7F3` |
| pink/200 | `#FBCFE8` |
| pink/300 | `#F9A8D4` |
| pink/400 | `#F472B6` |
| pink/500 | `#EC4899` |
| pink/600 | `#DB2777` |
| pink/700 | `#BE185D` |
| pink/800 | `#9D174D` |
| pink/900 | `#831843` |

**violet**

| Token | Hex |
|---|---|
| violet/500 | `#8B5CF6` |

**white**

| Token | Hex |
|---|---|
| white | `#FFFFFF` |

**black**

| Token | Hex |
|---|---|
| black | `#000000` |

## Typography

Font family：`Helvetica Neue`（fallback: sans-serif）

| Token | Size (px) | Weight | Line height |
|---|---|---|---|
| text-xs/Regular | 12 | 400 (Regular) | 1.5em |
| text-xs/Medium | 12 | 500 (Medium) | 1.5em |
| text-xs/Bold | 12 | 700 (Bold) | 1.5em |
| text-sm/Regular | 14 | 400 (Regular) | 1.5em |
| text-sm/Medium | 14 | 500 (Medium) | 1.5em |
| text-sm/Bold | 14 | 700 (Bold) | 1.5em |
| text-base/Light | 16 | 300 (Light) | 1.5em |
| text-base/Regular | 16 | 400 (Regular) | 1.5em |
| text-base/Medium | 16 | 500 (Medium) | 1.5em |
| text-base/Bold | 16 | 700 (Bold) | 1.5em |
| text-lg/Regular | 18 | 400 (Regular) | 1.5em |
| text-lg/Bold | 18 | 700 (Bold) | 1.5em |
| text-xl/Regular | 20 | 400 (Regular) | 1.5em |
| text-xl/Medium | 20 | 500 (Medium) | 1.5em |
| text-xl/Bold | 20 | 700 (Bold) | 1.5em |
| text-2xl/Regular | 24 | 400 (Regular) | 1.5em |
| text-3xl/Regular | 30 | 400 (Regular) | 1.5em |
| text-4xl/Regular | 36 | 400 (Regular) | 1.5em |
| text-5xl/Regular | 48 | 400 (Regular) | 1.5em |
| text-6xl/Regular | 64 | 400 (Regular) | 1.5em |

## Spacing

基礎間距單位（px），來自元件的 padding / gap：

1. 4
2. 8
3. 12
4. 16
5. 24

其餘在特定元件上觀察到的個別數值（非基礎刻度，僅供對照）：5, 6, 10, 11 px

## Effects (Shadows)

| Token | box-shadow |
|---|---|
| shadow-lg | `0px 4px 6px 0px rgba(0, 0, 0, 0.05), 0px 10px 15px 0px rgba(0, 0, 0, 0.1)` |
| shadow-md | `0px 4px 6px 0px rgba(0, 0, 0, 0.1), 0px 2px 4px 0px rgba(0, 0, 0, 0.06)` |
| shadow-base | `0px 1px 3px 0px rgba(0, 0, 0, 0.1), 0px 1px 2px 0px rgba(0, 0, 0, 0.06)` |
| shadow-xl | `0px 10px 10px 0px rgba(0, 0, 0, 0.04), 0px 20px 25px 0px rgba(0, 0, 0, 0.1)` |
| shadow-2xl | `0px 25px 50px 0px rgba(0, 0, 0, 0.25)` |
| shadow-inner | `inset 0px 2px 4px 0px rgba(0, 0, 0, 0.06)` |

---
*此檔案由 Claude Code 透過 Figma API（figma-developer-mcp）自動蒸餾產生，之後如設計稿更新，重新執行同一流程覆蓋本檔即可。*
