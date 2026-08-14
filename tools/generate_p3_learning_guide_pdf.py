"""Generate the consolidated study guide with the P3 advanced chapter included."""

from pathlib import Path

import generate_learning_guide_pdf as guide


guide.PARTS += (
    guide.Part(
        "第六篇  P3 数论、位运算与高级算法",
        guide.REPOSITORY_ROOT / "docs" / "P3数论位运算与高级算法学习指导.md",
        "补齐数论与位运算，形成高级图、字符串和动态规划算法族，并讲解质量门禁、结构化追踪和 14 道非重复专题题。",
    ),
)


if __name__ == "__main__":
    guide.main()
