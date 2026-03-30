# UI/UX Review Prompt — Web Application

Use this guide to evaluate the web application from a UI/UX perspective. Our user base is roughly **50% mobile and 50% desktop**, so every section must be evaluated on both form factors. Work through each section methodically, noting specific observations and recommendations as you go. Where possible, include screenshots or screen recordings to support your findings.

---

## Before You Begin

- **Complete this review twice** — once on a desktop browser and once on a mobile device (or a realistic mobile emulator). Document findings separately for each.
- **Device & browser:** Note the device, screen size, browser, and OS you're testing on.
- **User context:** Imagine you are a first-time user with no prior training on this application.
- **Scoring:** For each item, rate severity on a scale of 1–4:
  - **1 — Cosmetic:** Minor issue, fix if time permits.
  - **2 — Minor:** Causes slight confusion or friction; should be addressed.
  - **3 — Major:** Significantly impairs the experience; needs priority attention.
  - **4 — Critical:** Prevents task completion or causes data loss; must fix before release.

---

## Section 1: First Impressions (spend no more than 30 seconds per device)

Without clicking or tapping anything, answer for **both desktop and mobile**:

1. What do you believe this application does?
2. What is the primary action the interface wants you to take?
3. Does the visual hierarchy guide your eye to the most important element first?
4. Does the page feel cluttered, balanced, or sparse?
5. Do you trust this application based on its appearance alone? Why or why not?
6. **Mobile-specific:** Is the most important content visible above the fold without scrolling? Does the layout feel purpose-built for mobile, or like a shrunken desktop page?

---

## Section 2: Usability & Task Flow

Complete each of the application's core tasks (e.g., sign up, search, create/edit/delete an item, adjust settings). For each task, document:

- **Task name:**
- **Steps taken:** List every click, scroll, or input required.
- **Time to complete:** Roughly how long did it take?
- **Confusion points:** Where did you hesitate, backtrack, or guess?
- **Error handling:** Did you encounter any errors? Were the error messages helpful and specific? Did the system tell you how to fix the problem?
- **Success feedback:** After completing the task, was it clear that it worked?

### Key usability questions

- Can you complete the most important task within 3 clicks/taps or fewer?
- Is the navigation structure intuitive? Can you always tell where you are?
- Are there any dead ends (pages with no clear next action)?
- Does the back button behave as expected throughout?
- Are form labels clear? Are required fields marked? Is inline validation present?
- Are loading states visible and appropriately communicated?

### Mobile-specific usability

- Are tap targets large enough and spaced far enough apart to avoid mis-taps? (Minimum 44×44 CSS pixels with adequate spacing between adjacent targets.)
- Are forms optimized for mobile input? (Correct keyboard types for email, phone, number fields; minimal typing required; smart defaults and auto-fill support.)
- Can you complete core tasks one-handed on a typical phone?
- Are important actions (CTAs, submit buttons) within comfortable thumb reach, or buried at the top of the screen?
- Do any modals, dropdowns, or popovers work well on small screens, or do they get cut off or become hard to dismiss?
- Is horizontal scrolling ever required to view content? (It shouldn't be.)
- Does the mobile experience prioritize the right content, or does it just stack the full desktop layout vertically?

---

## Section 3: Visual Design & Consistency

### Layout & spacing

- Is there a consistent grid or alignment system?
- Is whitespace used effectively, or do elements feel cramped or floating?
- Are related items visually grouped, and unrelated items clearly separated?

### Typography

- How many typefaces are used? (Ideally no more than 2–3.)
- Is the type hierarchy clear (headings vs. body vs. captions)?
- Is body text legible at default size without zooming on both desktop and mobile?
- Is line length comfortable for reading? (Desktop: roughly 50–75 characters per line. Mobile: text should not feel cramped or require excessive scrolling.)
- On mobile, is the base font size at least 16px to prevent unwanted auto-zoom on iOS?

### Color

- Is the color palette cohesive and intentional?
- Are interactive elements (links, buttons) visually distinct from static content?
- Is color used consistently to convey meaning (e.g., red for errors, green for success)?
- Does meaning rely solely on color, or are there supporting indicators (icons, text)?

### Iconography & imagery

- Are icons recognizable without labels? If not, are they labeled?
- Are images relevant and high quality, or do they feel like stock filler?
- Do all images have appropriate alt text?

### Consistency

- Do similar components (buttons, cards, modals) look and behave the same way across pages?
- Are interaction patterns consistent (e.g., editing always works the same way)?
- Does the design system feel unified, or are there noticeable inconsistencies?

---

## Section 4: Accessibility (WCAG 2.1 AA baseline)

### Perceivable

- Do all non-text elements (images, icons, charts) have meaningful alt text?
- Is there sufficient color contrast between text and background? (Use a contrast checker — aim for at least 4.5:1 for normal text, 3:1 for large text.)
- Can the page be zoomed to 200% without loss of content or functionality?
- Are captions or transcripts provided for any audio/video content?

### Operable

- Can every interactive element be reached and activated using only the keyboard?
- Is a visible focus indicator present on all focusable elements?
- Is the tab order logical and follows the visual layout?
- Are there any keyboard traps (places where focus gets stuck)?
- Do any elements flash or auto-animate in ways that could trigger seizures?
- Are touch targets at least 44×44 pixels on touch devices?

### Understandable

- Is the page language set in the HTML (`lang` attribute)?
- Are form inputs associated with visible labels (not just placeholders)?
- Are error messages presented near the relevant field and announced to screen readers?
- Does the interface behave predictably (no unexpected context changes on focus or input)?

### Robust

- Does the page validate with no major HTML errors?
- Are ARIA roles and attributes used correctly (or is semantic HTML preferred where possible)?
- Does the page work with a screen reader (e.g., VoiceOver, NVDA)?

---

## Section 5: Responsive Design & Cross-Browser Behavior

### Responsive layout

- Does the layout adapt gracefully at common breakpoints (320px, 375px, 428px for mobile; 768px for tablet; 1024px+ for desktop)?
- Is there a clear mobile navigation pattern (hamburger menu, bottom tab bar, etc.)? Is it easy to discover and use?
- Are data-heavy elements (tables, charts, dashboards) usable on mobile, or do they require horizontal scrolling or become unreadable?
- Are images and media properly sized for each viewport (no oversized downloads on mobile)?
- Does the viewport meta tag prevent unwanted zooming behavior while still allowing accessibility zoom?

### Mobile-specific interaction

- Do swipe gestures (if used) feel natural and have clear affordances?
- Are sticky headers, footers, or floating action buttons appropriately sized and positioned on mobile without obscuring content?
- Does the app handle orientation changes (portrait to landscape) gracefully?
- Is pull-to-refresh supported where users would expect it?
- Does the on-screen keyboard push content up correctly, or does it obscure active input fields?

### Cross-browser

- Does the application function correctly in Chrome, Firefox, Safari, and Edge on desktop?
- Does it function correctly in Safari on iOS and Chrome on Android (the two dominant mobile browsers)?
- Are there any rendering differences between mobile browsers (especially iOS Safari quirks like safe-area insets, 100vh behavior, or rubber-band scrolling)?

---

## Section 6: Performance Perception

- Does the application feel fast on both desktop and a mid-range mobile device? Are there noticeable delays on any interaction?
- Are skeleton screens, spinners, or progress indicators used during loading?
- Do images and media load efficiently, or do they cause layout shifts? (Pay special attention on mobile — are images appropriately compressed and responsively served?)
- Is there any jank or stutter during scrolling or animation, particularly on mobile?
- On a throttled mobile connection (3G), is the app still usable within a reasonable timeframe?
- Does the app work gracefully when switching between Wi-Fi and cellular, or during intermittent connectivity?

---

## Summary Template

After completing your review, fill out the following summary:

### Desktop findings

| Category | Top Issue | Severity (1–4) | Recommendation |
|---|---|---|---|
| First Impressions | | | |
| Usability & Task Flow | | | |
| Visual Design | | | |
| Accessibility | | | |
| Responsive / Cross-Browser | | | |
| Performance Perception | | | |

### Mobile findings

| Category | Top Issue | Severity (1–4) | Recommendation |
|---|---|---|---|
| First Impressions | | | |
| Usability & Task Flow | | | |
| Mobile-Specific Usability | | | |
| Visual Design | | | |
| Accessibility | | | |
| Responsive / Cross-Browser | | | |
| Performance Perception | | | |

### Overall assessment

- **Strongest aspect of the UI/UX:**
- **Most urgent improvement needed (desktop):**
- **Most urgent improvement needed (mobile):**
- **One quick win that would make a noticeable difference:**
- **Does the mobile experience feel like a first-class product, or an afterthought?** Explain.

### Reviewer info

- **Name:**
- **Date:**
- **Desktop device / Browser / OS:**
- **Mobile device / Browser / OS:**
- **Time spent on review:**
