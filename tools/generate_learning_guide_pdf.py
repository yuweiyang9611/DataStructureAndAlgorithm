"""Generate the consolidated C# data structures and algorithms study guide PDF.

The project intentionally keeps the source material in Markdown because Markdown is
pleasant to review in Git.  This script turns those documents into one printable,
bookmarked PDF while preserving tables, code blocks, lists, and heading hierarchy.

Run from the repository root with the bundled Codex Python runtime or any Python
environment that provides ReportLab::

    python tools/generate_learning_guide_pdf.py

The stable output path is::

    output/pdf/CSharp数据结构与算法完整学习指导.pdf
"""

from __future__ import annotations

import argparse
import html
import re
from dataclasses import dataclass
from datetime import date
from pathlib import Path
from typing import Iterable, Sequence

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate,
    CondPageBreak,
    Flowable,
    Frame,
    HRFlowable,
    KeepTogether,
    ListFlowable,
    ListItem,
    LongTable,
    Preformatted,
    PageBreak,
    PageTemplate,
    Paragraph,
    Spacer,
    Table,
    TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents


REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_OUTPUT = REPOSITORY_ROOT / "output" / "pdf" / "CSharp数据结构与算法完整学习指导.pdf"

PAGE_WIDTH, PAGE_HEIGHT = A4
LEFT_MARGIN = 18 * mm
RIGHT_MARGIN = 18 * mm
TOP_MARGIN = 21 * mm
BOTTOM_MARGIN = 18 * mm
CONTENT_WIDTH = PAGE_WIDTH - LEFT_MARGIN - RIGHT_MARGIN

NAVY = colors.HexColor("#17324D")
BLUE = colors.HexColor("#245C83")
TEAL = colors.HexColor("#168AAD")
ORANGE = colors.HexColor("#F4A261")
INK = colors.HexColor("#203040")
MUTED = colors.HexColor("#5E6E7E")
PALE_BLUE = colors.HexColor("#EAF3F8")
PALE_TEAL = colors.HexColor("#E7F5F5")
CODE_BACKGROUND = colors.HexColor("#F4F7F9")
RULE = colors.HexColor("#CAD7E1")


@dataclass(frozen=True)
class Part:
    """One Markdown source and its role in the consolidated book."""

    title: str
    source: Path
    description: str


PARTS = (
    Part(
        "第一篇  基础学习路线与 C# 最佳实践",
        REPOSITORY_ROOT / "docs" / "CSharp数据结构与算法学习指导.md",
        "从复杂度、不变量和测试开始，建立线性结构、树、图、排序、动态规划与回溯的完整基础。",
    ),
    Part(
        "第二篇  进阶数据结构与算法",
        REPOSITORY_ROOT / "docs" / "CSharp数据结构与算法进阶学习指导.md",
        "深入两种哈希策略、红黑树、跳表、非比较排序、区间结构与进阶算法，并补充 C# 14 工程实践。",
    ),
    Part(
        "第三篇  API 设计与比较器约定",
        REPOSITORY_ROOT / "docs" / "API设计与比较器约定.md",
        "统一空值、区间、比较器、所有权、数值和验证契约，说明现代 C# 写法为什么服务于可预测的 API。",
    ),
    Part(
        "第四篇  LeetCode 100 题题型地图",
        REPOSITORY_ROOT / "docs" / "LeetCode100题完整学习指导.md",
        "按题型说明每道题为什么采用当前方法、需要维护什么不变量，以及复杂度从何而来。",
    ),
    Part(
        "第五篇  核心 19 题精讲",
        REPOSITORY_ROOT / "docs" / "LeetCode经典题目学习指导.md",
        "选取 19 道高频题展开推理过程，帮助把题型识别落实为可独立书写、可测试的 C# 代码。",
    ),
)


def register_fonts() -> None:
    """Register fonts that cover both Chinese prose and C# comments.

    ReportLab does not perform browser-like font fallback.  Using a Latin-only
    monospace font for code would therefore turn Chinese comments into empty boxes.
    Microsoft YaHei is used for both prose and code so every teaching comment remains
    readable.  SimHei is retained as a fallback on Windows editions without YaHei.
    """

    font_dir = Path("C:/Windows/Fonts")
    candidates = (
        (font_dir / "msyh.ttc", font_dir / "msyhbd.ttc"),
        (font_dir / "simhei.ttf", font_dir / "simhei.ttf"),
    )

    for regular, bold in candidates:
        if regular.exists() and bold.exists():
            pdfmetrics.registerFont(TTFont("StudySans", str(regular), subfontIndex=0))
            pdfmetrics.registerFont(TTFont("StudySans-Bold", str(bold), subfontIndex=0))
            pdfmetrics.registerFont(TTFont("StudyCode", str(regular), subfontIndex=0))
            pdfmetrics.registerFontFamily(
                "StudySans",
                normal="StudySans",
                bold="StudySans-Bold",
                italic="StudySans",
                boldItalic="StudySans-Bold",
            )
            return

    raise FileNotFoundError(
        "找不到微软雅黑或黑体。请在 Windows 字体目录安装 msyh.ttc 或 simhei.ttf。"
    )


def build_styles() -> dict[str, ParagraphStyle]:
    """Create a restrained print-oriented style system."""

    sample = getSampleStyleSheet()
    body = ParagraphStyle(
        "BodyCJK",
        parent=sample["BodyText"],
        fontName="StudySans",
        fontSize=9.2,
        leading=15.2,
        textColor=INK,
        alignment=TA_LEFT,
        wordWrap="CJK",
        spaceAfter=5.5,
        orphanLines=2,
        widowLines=2,
    )

    return {
        "body": body,
        "small": ParagraphStyle(
            "SmallCJK",
            parent=body,
            fontSize=7.5,
            leading=10.4,
            textColor=MUTED,
            spaceAfter=3,
        ),
        "caption": ParagraphStyle(
            "CaptionCJK",
            parent=body,
            fontSize=7.2,
            leading=9.5,
            textColor=MUTED,
            spaceAfter=2,
        ),
        "part": ParagraphStyle(
            "PartTitle",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=20,
            leading=28,
            textColor=NAVY,
            spaceBefore=4,
            spaceAfter=10,
            keepWithNext=True,
        ),
        "h2": ParagraphStyle(
            "Heading2CJK",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=14.2,
            leading=20,
            textColor=BLUE,
            borderColor=TEAL,
            borderWidth=0,
            borderPadding=(0, 0, 3, 0),
            spaceBefore=13,
            spaceAfter=7,
            keepWithNext=True,
        ),
        "h3": ParagraphStyle(
            "Heading3CJK",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=11.2,
            leading=16,
            textColor=NAVY,
            spaceBefore=9,
            spaceAfter=5,
            keepWithNext=True,
        ),
        "h4": ParagraphStyle(
            "Heading4CJK",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=9.6,
            leading=14,
            textColor=TEAL,
            spaceBefore=7,
            spaceAfter=4,
            keepWithNext=True,
        ),
        "quote": ParagraphStyle(
            "QuoteCJK",
            parent=body,
            leftIndent=8 * mm,
            rightIndent=4 * mm,
            borderColor=TEAL,
            borderWidth=1.5,
            borderPadding=(4, 8, 4, 8),
            backColor=PALE_TEAL,
            textColor=colors.HexColor("#28535A"),
            spaceBefore=4,
            spaceAfter=7,
        ),
        "list": ParagraphStyle(
            "ListCJK",
            parent=body,
            fontSize=9.0,
            leading=13.7,
            leftIndent=0,
            firstLineIndent=0,
            spaceAfter=1.5,
        ),
        "code": ParagraphStyle(
            "CodeCJK",
            fontName="StudyCode",
            fontSize=7.1,
            leading=10.2,
            textColor=colors.HexColor("#243746"),
            leftIndent=0,
            rightIndent=0,
            wordWrap="CJK",
        ),
        "code_label": ParagraphStyle(
            "CodeLabel",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=6.6,
            leading=8,
            textColor=colors.white,
            backColor=BLUE,
            borderPadding=(2, 5, 2, 5),
            spaceAfter=0,
        ),
        "table_header": ParagraphStyle(
            "TableHeaderCJK",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=7.1,
            leading=9.4,
            textColor=colors.white,
            spaceAfter=0,
        ),
        "table_cell": ParagraphStyle(
            "TableCellCJK",
            parent=body,
            fontSize=6.9,
            leading=9.5,
            textColor=INK,
            spaceAfter=0,
        ),
        "cover_title": ParagraphStyle(
            "CoverTitle",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=28,
            leading=40,
            textColor=colors.white,
            alignment=TA_CENTER,
            spaceAfter=8,
        ),
        "cover_subtitle": ParagraphStyle(
            "CoverSubtitle",
            parent=body,
            fontSize=13,
            leading=21,
            textColor=colors.HexColor("#D9EAF3"),
            alignment=TA_CENTER,
            spaceAfter=10,
        ),
        "cover_meta": ParagraphStyle(
            "CoverMeta",
            parent=body,
            fontSize=9,
            leading=14,
            textColor=colors.white,
            alignment=TA_CENTER,
            spaceAfter=0,
        ),
        "toc_title": ParagraphStyle(
            "TocTitle",
            parent=body,
            fontName="StudySans-Bold",
            fontSize=19,
            leading=26,
            textColor=NAVY,
            spaceAfter=14,
        ),
    }


class StudyGuideDocTemplate(BaseDocTemplate):
    """Document template that creates PDF outlines and table-of-contents entries."""

    def __init__(self, filename: str, *, styles: dict[str, ParagraphStyle]) -> None:
        super().__init__(
            filename,
            pagesize=A4,
            leftMargin=LEFT_MARGIN,
            rightMargin=RIGHT_MARGIN,
            topMargin=TOP_MARGIN,
            bottomMargin=BOTTOM_MARGIN,
            title="C# 数据结构与算法完整学习指导",
            author="DataStructureAndAlgorithm 学习项目",
            subject=".NET 10、C# 14、数据结构、算法与 LeetCode 100 题",
            creator="DataStructureAndAlgorithm PDF Generator",
        )
        self.styles = styles
        self._bookmark_index = 0

        frame = Frame(
            LEFT_MARGIN,
            BOTTOM_MARGIN,
            CONTENT_WIDTH,
            PAGE_HEIGHT - TOP_MARGIN - BOTTOM_MARGIN,
            id="content",
        )
        self.addPageTemplates(PageTemplate(id="main", frames=[frame], onPage=self._draw_page))

    def beforeDocument(self) -> None:
        """Keep bookmark keys stable across TableOfContents layout passes."""

        super().beforeDocument()
        self._bookmark_index = 0

    def _draw_page(self, canvas, document) -> None:  # noqa: ANN001 - ReportLab callback
        canvas.saveState()

        if document.page == 1:
            canvas.setFillColor(NAVY)
            canvas.rect(0, 0, PAGE_WIDTH, PAGE_HEIGHT, fill=1, stroke=0)
            canvas.setFillColor(TEAL)
            canvas.rect(0, 0, PAGE_WIDTH, 12 * mm, fill=1, stroke=0)
            canvas.setFillColor(ORANGE)
            canvas.rect(0, PAGE_HEIGHT - 7 * mm, PAGE_WIDTH, 7 * mm, fill=1, stroke=0)
        else:
            canvas.setStrokeColor(RULE)
            canvas.setLineWidth(0.45)
            canvas.line(LEFT_MARGIN, PAGE_HEIGHT - 13 * mm, PAGE_WIDTH - RIGHT_MARGIN, PAGE_HEIGHT - 13 * mm)
            canvas.line(LEFT_MARGIN, 12 * mm, PAGE_WIDTH - RIGHT_MARGIN, 12 * mm)

            canvas.setFont("StudySans", 7.2)
            canvas.setFillColor(MUTED)
            canvas.drawString(LEFT_MARGIN, PAGE_HEIGHT - 10 * mm, "C# 数据结构与算法完整学习指导")
            canvas.drawRightString(
                PAGE_WIDTH - RIGHT_MARGIN,
                PAGE_HEIGHT - 10 * mm,
                ".NET 10 / C# 14",
            )
            canvas.drawCentredString(PAGE_WIDTH / 2, 8.2 * mm, f"第 {document.page - 1} 页")

        canvas.restoreState()

    def afterFlowable(self, flowable: Flowable) -> None:
        if not isinstance(flowable, Paragraph):
            return

        level_by_style = {
            "PartTitle": 0,
            "Heading2CJK": 1,
            "Heading3CJK": 2,
        }
        level = level_by_style.get(flowable.style.name)
        if level is None:
            return

        title = flowable.getPlainText()
        key = f"heading-{self._bookmark_index}"
        self._bookmark_index += 1
        self.canv.bookmarkPage(key)
        self.canv.addOutlineEntry(title, key, level=level, closed=level > 0)
        if level <= 1:
            self.notify("TOCEntry", (level, title, self.page - 1, key))


def _extract_inline_tokens(text: str) -> tuple[str, dict[str, str]]:
    """Protect Markdown inline constructs before XML escaping for Paragraph."""

    replacements: dict[str, str] = {}

    def protect(markup: str) -> str:
        token = f"@@INLINE{len(replacements)}@@"
        replacements[token] = markup
        return token

    def link_replacement(match: re.Match[str]) -> str:
        label, target = match.group(1), match.group(2)
        safe_label = html.escape(label)
        if target.startswith(("https://", "http://")):
            safe_target = html.escape(target, quote=True)
            return protect(f'<link href="{safe_target}" color="#168AAD">{safe_label}</link>')
        return protect(f'<font color="#245C83">{safe_label}</font>')

    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", link_replacement, text)
    text = re.sub(
        r"`([^`]+)`",
        lambda match: protect(
            f'<font name="StudyCode" color="#8A3B12">{html.escape(match.group(1))}</font>'
        ),
        text,
    )
    text = re.sub(
        r"\*\*([^*]+)\*\*",
        lambda match: protect(f"<b>{html.escape(match.group(1))}</b>"),
        text,
    )
    return text, replacements


def inline_markup(text: str) -> str:
    """Translate the small inline-Markdown subset used by the guides."""

    protected, replacements = _extract_inline_tokens(text.strip())
    escaped = html.escape(protected).replace("  ", " &nbsp;")
    for token, markup in replacements.items():
        escaped = escaped.replace(token, markup)
    return escaped


def split_table_row(line: str) -> list[str]:
    """Split a Markdown table row while allowing escaped pipe characters."""

    stripped = line.strip().strip("|")
    cells = re.split(r"(?<!\\)\|", stripped)
    return [cell.strip().replace(r"\|", "|") for cell in cells]


def is_table_separator(line: str) -> bool:
    cells = split_table_row(line)
    return bool(cells) and all(re.fullmatch(r":?-{3,}:?", cell.replace(" ", "")) for cell in cells)


def table_widths(headers: Sequence[str], column_count: int) -> list[float]:
    """Use semantic widths for the wide explanatory tables in this repository."""

    joined = " ".join(headers)
    if column_count == 5 and "为什么" in joined:
        ratios = (0.07, 0.19, 0.29, 0.28, 0.17)
    elif column_count == 5:
        ratios = (0.08, 0.24, 0.15, 0.35, 0.18)
    elif column_count == 4:
        ratios = (0.18, 0.25, 0.36, 0.21)
    elif column_count == 3:
        ratios = (0.18, 0.40, 0.42)
    elif column_count == 2:
        ratios = (0.37, 0.63)
    else:
        ratios = tuple(1 / column_count for _ in range(column_count))
    return [CONTENT_WIDTH * ratio for ratio in ratios]


def build_table(rows: Sequence[Sequence[str]], styles: dict[str, ParagraphStyle]) -> LongTable:
    """Build a page-splitting table with repeating headers."""

    headers = rows[0]
    paragraph_rows: list[list[Paragraph]] = []
    for row_index, row in enumerate(rows):
        style = styles["table_header"] if row_index == 0 else styles["table_cell"]
        normalized = list(row) + [""] * (len(headers) - len(row))
        paragraph_rows.append([Paragraph(inline_markup(cell), style) for cell in normalized[: len(headers)]])

    table = LongTable(
        paragraph_rows,
        colWidths=table_widths(headers, len(headers)),
        repeatRows=1,
        hAlign="LEFT",
        splitByRow=1,
        spaceBefore=5,
        spaceAfter=8,
    )
    table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), BLUE),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 4),
                ("RIGHTPADDING", (0, 0), (-1, -1), 4),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE_BLUE]),
                ("GRID", (0, 0), (-1, -1), 0.35, RULE),
                ("LINEBELOW", (0, 0), (-1, 0), 0.8, TEAL),
            ]
        )
    )
    return table


def build_code_block(language: str, code: str, styles: dict[str, ParagraphStyle]) -> KeepTogether:
    """Build a compact labelled code block that keeps Chinese comments readable."""

    label = language.upper() if language else "CODE"
    preformatted = Preformatted(code.rstrip(), styles["code"], maxLineLength=92)
    content = Table(
        [[preformatted]],
        colWidths=[CONTENT_WIDTH],
        style=TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), CODE_BACKGROUND),
                ("BOX", (0, 0), (-1, -1), 0.55, RULE),
                ("LEFTPADDING", (0, 0), (-1, -1), 7),
                ("RIGHTPADDING", (0, 0), (-1, -1), 7),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
            ]
        ),
    )
    label_table = Table(
        [[Paragraph(html.escape(label), styles["code_label"])]],
        colWidths=[CONTENT_WIDTH],
        style=TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, -1), BLUE),
                ("LEFTPADDING", (0, 0), (-1, -1), 0),
                ("RIGHTPADDING", (0, 0), (-1, -1), 0),
                ("TOPPADDING", (0, 0), (-1, -1), 0),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 0),
            ]
        ),
    )
    return KeepTogether([Spacer(1, 2), label_table, content, Spacer(1, 7)])


def build_list(
    items: Sequence[str],
    *,
    ordered: bool,
    styles: dict[str, ParagraphStyle],
) -> ListFlowable:
    """Build a compact one-level list; current guides do not require deep nesting."""

    list_items = [
        ListItem(Paragraph(inline_markup(item), styles["list"]), leftIndent=12)
        for item in items
    ]
    return ListFlowable(
        list_items,
        bulletType="1" if ordered else "bullet",
        start="1" if ordered else None,
        leftIndent=14,
        bulletFontName="StudySans",
        bulletFontSize=8,
        bulletColor=TEAL,
        spaceBefore=1,
        spaceAfter=6,
    )


def markdown_to_flowables(markdown: str, styles: dict[str, ParagraphStyle]) -> list[Flowable]:
    """Convert the deliberately small Markdown dialect used by this repository."""

    lines = markdown.replace("\r\n", "\n").split("\n")
    flowables: list[Flowable] = []
    paragraph_lines: list[str] = []

    def flush_paragraph() -> None:
        if not paragraph_lines:
            return
        text = " ".join(part.strip() for part in paragraph_lines if part.strip())
        paragraph_lines.clear()
        if text:
            flowables.append(Paragraph(inline_markup(text), styles["body"]))

    index = 0
    while index < len(lines):
        line = lines[index]
        stripped = line.strip()

        if not stripped:
            flush_paragraph()
            index += 1
            continue

        if stripped.startswith("```"):
            flush_paragraph()
            language = stripped[3:].strip()
            index += 1
            code_lines: list[str] = []
            while index < len(lines) and not lines[index].strip().startswith("```"):
                code_lines.append(lines[index].expandtabs(4))
                index += 1
            index += 1 if index < len(lines) else 0
            flowables.append(build_code_block(language, "\n".join(code_lines), styles))
            continue

        heading = re.match(r"^(#{1,4})\s+(.+)$", stripped)
        if heading:
            flush_paragraph()
            level = len(heading.group(1))
            title = re.sub(r"\s+\{#[^}]+\}\s*$", "", heading.group(2)).strip()
            if level == 1:
                # The consolidated book already supplies a part title, so the source
                # document title would only repeat the same information.
                index += 1
                continue
            style_key = {2: "h2", 3: "h3", 4: "h4"}[level]
            flowables.append(CondPageBreak(28 * mm if level == 2 else 18 * mm))
            flowables.append(Paragraph(inline_markup(title), styles[style_key]))
            if level == 2:
                flowables.append(HRFlowable(width="100%", thickness=0.55, color=RULE, spaceAfter=5))
            index += 1
            continue

        if stripped.startswith("|") and index + 1 < len(lines) and is_table_separator(lines[index + 1]):
            flush_paragraph()
            rows = [split_table_row(line)]
            index += 2
            while index < len(lines) and lines[index].strip().startswith("|"):
                rows.append(split_table_row(lines[index]))
                index += 1
            flowables.append(build_table(rows, styles))
            continue

        list_match = re.match(r"^\s*([-*]|\d+\.)\s+(.+)$", line)
        if list_match:
            flush_paragraph()
            ordered = list_match.group(1).endswith(".")
            items: list[str] = []
            while index < len(lines):
                match = re.match(r"^\s*([-*]|\d+\.)\s+(.+)$", lines[index])
                if match is None or match.group(1).endswith(".") != ordered:
                    break
                items.append(match.group(2).strip())
                index += 1
            flowables.append(build_list(items, ordered=ordered, styles=styles))
            continue

        if stripped.startswith(">"):
            flush_paragraph()
            quote_lines: list[str] = []
            while index < len(lines) and lines[index].strip().startswith(">"):
                quote_lines.append(lines[index].strip().lstrip(">").strip())
                index += 1
            flowables.append(Paragraph(inline_markup(" ".join(quote_lines)), styles["quote"]))
            continue

        if re.fullmatch(r"-{3,}", stripped):
            flush_paragraph()
            flowables.append(HRFlowable(width="100%", thickness=0.7, color=RULE, spaceBefore=5, spaceAfter=7))
            index += 1
            continue

        paragraph_lines.append(stripped)
        index += 1

    flush_paragraph()
    return flowables


def cover_story(styles: dict[str, ParagraphStyle]) -> list[Flowable]:
    """Create a cover that communicates scope without looking like a source dump."""

    badge_style = ParagraphStyle(
        "CoverBadge",
        parent=styles["cover_meta"],
        fontName="StudySans-Bold",
        fontSize=9,
        leading=13,
        textColor=NAVY,
        backColor=colors.white,
        borderPadding=(5, 8, 5, 8),
    )
    badges = Table(
        [
            [
                Paragraph(".NET 10", badge_style),
                Paragraph("C# 14", badge_style),
                Paragraph("LeetCode 100 题", badge_style),
            ]
        ],
        colWidths=[CONTENT_WIDTH / 3 - 4 * mm] * 3,
        hAlign="CENTER",
        style=TableStyle(
            [
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("ALIGN", (0, 0), (-1, -1), "CENTER"),
                ("LEFTPADDING", (0, 0), (-1, -1), 3 * mm),
                ("RIGHTPADDING", (0, 0), (-1, -1), 3 * mm),
            ]
        ),
    )

    return [
        Spacer(1, 43 * mm),
        Paragraph("C# 数据结构与算法", styles["cover_title"]),
        Paragraph("完整学习指导", styles["cover_title"]),
        Spacer(1, 3 * mm),
        HRFlowable(width="42%", thickness=2, color=ORANGE, hAlign="CENTER", spaceAfter=9 * mm),
        Paragraph(
            "从不变量与复杂度，到工程实现与经典题型<br/>"
            "结合项目源码、测试和现代 C# 语法循序学习",
            styles["cover_subtitle"],
        ),
        Spacer(1, 10 * mm),
        badges,
        Spacer(1, 24 * mm),
        Paragraph("DataStructureAndAlgorithm 学习项目", styles["cover_meta"]),
        Paragraph(f"生成日期：{date.today().isoformat()}", styles["cover_meta"]),
        PageBreak(),
    ]


def front_matter(styles: dict[str, ParagraphStyle]) -> list[Flowable]:
    """Create edition notes and a generated table of contents."""

    toc = TableOfContents()
    toc.levelStyles = [
        ParagraphStyle(
            "TocLevel0",
            fontName="StudySans-Bold",
            fontSize=10.5,
            leading=16,
            leftIndent=0,
            firstLineIndent=0,
            textColor=NAVY,
            spaceBefore=7,
        ),
        ParagraphStyle(
            "TocLevel1",
            fontName="StudySans",
            fontSize=8.6,
            leading=13,
            leftIndent=8 * mm,
            firstLineIndent=0,
            textColor=INK,
            spaceBefore=2,
        ),
        ParagraphStyle(
            "TocLevel2",
            fontName="StudySans",
            fontSize=7.7,
            leading=11.5,
            leftIndent=15 * mm,
            firstLineIndent=0,
            textColor=MUTED,
            spaceBefore=1,
        ),
    ]
    toc.dotsMinLevel = 0

    return [
        Paragraph("编排说明", styles["toc_title"]),
        Paragraph(
            f"本 PDF 将当前生成入口选择的 {len(PARTS)} 份学习文档合并为一册。各篇按从基础方法、"
            "进阶结构和 API 契约，到经典题、高级专题与综合应用的顺序编排：先形成可复用的思考框架，"
            "再通过题目和真实场景训练算法选型与组合不变量。",
            styles["body"],
        ),
        Paragraph(
            "源码仍是最终实现依据。阅读时建议同时打开对应的 C# 文件和 xUnit 测试，先手算样例，"
            "再运行测试，最后尝试在不看答案的情况下重写核心步骤。",
            styles["quote"],
        ),
        Spacer(1, 5 * mm),
        Paragraph("目录", styles["toc_title"]),
        toc,
        PageBreak(),
    ]


def build_story(styles: dict[str, ParagraphStyle]) -> list[Flowable]:
    """Compose the cover, front matter, and all Markdown parts selected by the caller."""

    story: list[Flowable] = []
    story.extend(cover_story(styles))
    story.extend(front_matter(styles))

    for part_index, part in enumerate(PARTS):
        if not part.source.exists():
            raise FileNotFoundError(f"缺少学习指导源文件：{part.source}")

        if part_index > 0:
            story.append(PageBreak())
        story.append(Paragraph(inline_markup(part.title), styles["part"]))
        story.append(HRFlowable(width="100%", thickness=2, color=TEAL, spaceAfter=6))
        story.append(Paragraph(inline_markup(part.description), styles["quote"]))
        story.extend(markdown_to_flowables(part.source.read_text(encoding="utf-8"), styles))

    return story


def validate_sources() -> None:
    """Fail early with an actionable message instead of producing a partial book."""

    missing = [str(part.source) for part in PARTS if not part.source.exists()]
    if missing:
        joined = "\n".join(f"- {path}" for path in missing)
        raise FileNotFoundError(f"以下学习指导不存在：\n{joined}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--output",
        type=Path,
        default=DEFAULT_OUTPUT,
        help=f"PDF output path (default: {DEFAULT_OUTPUT})",
    )
    return parser.parse_args()


def main() -> None:
    args = parse_args()
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)

    validate_sources()
    register_fonts()
    styles = build_styles()
    document = StudyGuideDocTemplate(str(output), styles=styles)
    document.multiBuild(build_story(styles))

    print(f"Generated: {output}")
    print(f"Bytes: {output.stat().st_size}")


if __name__ == "__main__":
    main()
