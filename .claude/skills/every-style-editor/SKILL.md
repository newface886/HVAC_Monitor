# every-style-editor

## Overview

**Type**: Content  
**Category**: Writing & Editing  
**Purpose**: Perform comprehensive four-phase line-by-line editorial review to ensure content matches style guide requirements

Think of this skill as having a tireless copy editor who never misses a comma splice, never forgets capitalization rules, and always catches formatting inconsistencies. When you have a draft that needs professional polish before publication, invoke this skill.

---

## When to Use

| Scenario | Use This Skill |
|----------|---------------|
| Blog post or article draft | ✅ Yes |
| Marketing copy review | ✅ Yes |
| Technical documentation | ✅ Yes |
| Email or message to stakeholders | ✅ Yes |
| Social media content | ✅ Yes |
| Code comments or commit messages | ✅ Yes |
| First draft brainstorming | ❌ No (too early) |
| Final proofread before publish | ✅ Yes |

---

## Core Capabilities

### Four-Phase Review Process

This skill follows a systematic four-phase approach to editorial review:

#### Phase 1: Initial Assessment
Understand the context before making any changes.

| Check | Description |
|-------|-------------|
| **Context** | What is this content for? (blog, email, docs, marketing) |
| **Type** | What format is it? (long-form, short-form, technical, casual) |
| **Audience** | Who will read this? (developers, executives, general public) |
| **Tone** | What feeling should it evoke? (professional, friendly, authoritative) |

#### Phase 2: Detailed Line Edit
Examine every sentence for clarity and correctness.

| Check | Description |
|-------|-------------|
| **Sentence Structure** | Run-on sentences, fragments, passive voice overuse |
| **Punctuation** | Commas, semicolons, colons, dashes, quotation marks |
| **Capitalization** | Proper nouns, titles, headings, brand names |
| **Word Choice** | Redundancy, jargon, clarity, precision |
| **Flow** | Transitions between paragraphs and sections |

#### Phase 3: Mechanical Review
Check formatting and consistency details.

| Check | Description |
|-------|-------------|
| **Spacing** | Single vs double spaces, paragraph breaks |
| **Formatting** | Bold, italic, lists, headers consistency |
| **Consistency** | Terminology, date formats, number styles |
| **Links** | URL formatting, anchor text, broken links |
| **Visual Elements** | Images, tables, code blocks alignment |

#### Phase 4: Recommendations
Provide actionable improvement suggestions.

| Check | Description |
|-------|-------------|
| **Strengths** | What's working well |
| **Weaknesses** | What needs improvement |
| **Suggestions** | Specific, actionable changes |
| **Priority** | Critical vs nice-to-have fixes |

---

## Style Check Categories

### Grammar and Punctuation
- Subject-verb agreement
- Pronoun clarity and agreement
- Comma usage (Oxford comma consistency)
- Semicolon and colon usage
- Dash vs hyphen usage
- Quotation mark style (single vs double)
- Apostrophe usage

### Style Guide Compliance
- Capitalization rules (internet, web, email, etc.)
- Number formatting (digits vs words)
- Date and time formats
- Currency and measurement units
- Abbreviation and acronym usage
- Voice and tense consistency

### Word Choice Optimization
- Remove filler words (very, really, just, actually)
- Replace weak verbs with strong verbs
- Eliminate redundancy and repetition
- Clarify ambiguous phrasing
- Adjust reading level for audience
- Remove unnecessary jargon

### Formatting Consistency
- Header hierarchy (H1, H2, H3)
- List formatting (bullets vs numbers)
- Code block styling
- Link formatting
- Image captions
- Table styling

---

## Workflow

### Step 1: Receive Content
```
User provides draft content for review
```

### Step 2: Initial Assessment
```
Analyze context, type, audience, and tone
Document findings before making edits
```

### Step 3: Four-Phase Review
```
Execute Phase 1-4 systematically
Track all changes and reasoning
```

### Step 4: Deliver Output
```
Provide edited version with change tracking
Include summary of major changes
List recommendations for future improvements
```

---

## Output Format

### Edited Content Section
```markdown
## Edited Version

[Full edited content with changes tracked]

**Change Legend:**
- ~~strikethrough~~ = removed text
- **bold** = added or modified text
- [comment] = editorial note
```

### Review Summary Section
```markdown
## Review Summary

### Phase 1: Initial Assessment
- Context: [description]
- Type: [description]
- Audience: [description]
- Tone: [description]

### Phase 2: Line Edits
- Total sentences reviewed: [number]
- Sentences modified: [number]
- Common issues: [list]

### Phase 3: Mechanical Checks
- Formatting issues found: [number]
- Consistency issues found: [number]
- Resolved: [number]

### Phase 4: Recommendations
**Strengths:**
- [list]

**Areas for Improvement:**
- [list]

**Priority Actions:**
1. [critical fix]
2. [important fix]
3. [nice-to-have]
```

---

## Examples

### Example 1: Blog Post Review

**User Input:**
```
skill: every-style-editor

Please review this blog post draft:

# Getting Started with Rails

Rails is a web framework. It was created by DHH. Many people use it. 
You should try it because its really good. The community are very helpful 
and there is lots of documentation available on the internet.

To install, you run: gem install rails. Then you create a new app with 
rails new myapp. Its that simple!
```

**Skill Output:**
```markdown
## Edited Version

# Getting Started with Rails

Rails is a web application framework created by David Heinemeier Hansson (DHH). 
Thousands of developers use it worldwide.

You should try it because **it's** really good. The community **is** very helpful, 
and there **are many** documentation resources available on the **Internet**.

To install, run: `gem install rails`. Then create a new app with 
`rails new myapp`. **It's** that simple!

**Change Legend:**
- ~~Its~~ → **It's** (contraction correction)
- ~~community are~~ → **community is** (subject-verb agreement)
- ~~lots of~~ → **many** (formal word choice)
- ~~internet~~ → **Internet** (capitalization)
- Added code formatting for commands
```

```markdown
## Review Summary

### Phase 1: Initial Assessment
- Context: Technical blog post for developers
- Type: Tutorial/getting started guide
- Audience: Developers new to Rails
- Tone: Friendly, encouraging, informative

### Phase 2: Line Edits
- Total sentences reviewed: 7
- Sentences modified: 5
- Common issues: Contractions, subject-verb agreement, capitalization

### Phase 3: Mechanical Checks
- Formatting issues found: 2 (code blocks missing backticks)
- Consistency issues found: 3 (capitalization, contractions)
- Resolved: 5

### Phase 4: Recommendations
**Strengths:**
- Clear, concise instructions
- Friendly tone appropriate for beginners
- Logical flow from introduction to installation

**Areas for Improvement:**
- Add code syntax highlighting
- Include version requirements
- Add troubleshooting section

**Priority Actions:**
1. Fix all contraction apostrophes (critical)
2. Add code block formatting (critical)
3. Expand troubleshooting section (important)
```

### Example 2: Email Review

**User Input:**
```
skill: every-style-editor

Please review this email to stakeholders:

Hi Team,

Just wanted to give everyone a quick update on the project. 
We're making good progress and should be on track for the Q4 launch. 
There was some issues with the database migration but we fixed it.

Let me know if you have any questions.

Thanks,
[Name]
```

**Skill Output:**
```markdown
## Edited Version

Hi Team,

I wanted to provide a quick update on the project. 
We're making good progress and should be on track for the Q4 launch. 
There **were some issues** with the database migration, but we **have resolved them**.

Please let me know if you have any questions.

Thanks,
[Name]

**Changes:**
- ~~Just wanted~~ → **I wanted** (complete sentence)
- ~~give everyone~~ → **provide** (more professional)
- ~~was some issues~~ → **were some issues** (subject-verb agreement)
- ~~fixed it~~ → **have resolved them** (professional phrasing, pronoun agreement)
- ~~Let me know~~ → **Please let me know** (politeness)
```

---

## Special Considerations

### Technical Content
- Preserve code accuracy (never change code logic)
- Maintain technical terminology consistency
- Keep command examples in code blocks
- Verify version numbers and paths

### Marketing Content
- Maintain brand voice consistency
- Preserve call-to-action clarity
- Keep headline impact
- Ensure compliance claims are accurate

### Legal/Compliance Content
- Never modify legal language without review
- Flag potential compliance issues
- Preserve disclaimer wording
- Note jurisdiction-specific requirements

### International Content
- Check date formats (MM/DD vs DD/MM)
- Verify currency symbols and formatting
- Note region-specific terminology
- Flag potential translation issues

---

## Common Style Decisions

| Element | Standard | Alternative |
|---------|----------|-------------|
| Oxford Comma | Use consistently | Omit consistently |
| Internet | Capitalized | Lowercase |
| Email | Lowercase | Hyphenated (e-mail) |
| Web | Lowercase | Capitalized |
| Numbers 1-9 | Spell out | Use digits |
| Numbers 10+ | Use digits | Spell out |
| Date Format | YYYY-MM-DD | MM/DD/YYYY |
| Quotation Marks | Double ("") | Single ('') |

---

## Integration

### With Other Skills
- `compound-docs`: Document style decisions for team reference
- `file-todos`: Track editorial tasks and revisions
- `frontend-design`: Ensure UI copy matches design tone

### With Commands
- `/review`: Use for code review comments
- `/work`: Use for documentation during development

---

## Requirements

| Requirement | Description |
|-------------|-------------|
| Content to Review | Draft text, article, email, or document |
| Style Guide (Optional) | Specific style guide to follow (AP, Chicago, custom) |
| Audience Context | Who will read this content |
| Publication Medium | Where this will be published (web, print, email) |

---

## Best Practices

### For Users
1. Provide context about audience and purpose
2. Share any applicable style guides
3. Specify must-keep phrases or terminology
4. Indicate urgency and depth of review needed

### For AI
1. Always explain why changes are made
2. Preserve author voice while improving clarity
3. Flag uncertain changes for user review
4. Prioritize critical fixes over nice-to-haves
5. Provide before/after comparisons for major changes

---