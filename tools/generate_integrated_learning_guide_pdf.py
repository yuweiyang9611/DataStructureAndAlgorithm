"""Generate the complete study guide with P3 and integrated scenarios included.

The smaller generators stay composable: the base module defines the common PDF
layout, the P3 wrapper appends the advanced chapter, and this final wrapper appends
the integrated-project chapter.  Keeping content selection separate from layout
makes it obvious why a chapter appears in the printed guide.
"""

import generate_p3_learning_guide_pdf as p3


p3.guide.PARTS += (
    p3.guide.Part(
        "第七篇  数据结构与算法综合项目实战",
        p3.guide.REPOSITORY_ROOT / "docs" / "综合项目实战学习指导.md",
        "通过城市配送、迷你全文检索、项目资源排程和迷你存储引擎，把自实现结构组合成端到端应用，并用版本化追踪、模型测试、跨平台 CI 与基准趋势持续证明跨模块不变量。",
    ),
)


if __name__ == "__main__":
    p3.guide.main()
