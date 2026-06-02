from datetime import datetime
from reportlab.lib import colors
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm
from reportlab.platypus import Paragraph, Preformatted, SimpleDocTemplate, Spacer, Table, TableStyle, PageBreak
from reportlab.graphics.shapes import Drawing, Rect, String, Line, Polygon


def p(text, style):
    return Paragraph(text, style)


def _sysml_block(x, y, w, h, label, fill=colors.whitesmoke):
    return [
        Rect(x, y, w, h, strokeColor=colors.black, fillColor=fill, strokeWidth=1),
        String(x + 6, y + h / 2, label, fontName="Helvetica", fontSize=8),
    ]


def make_bdd_diagram():
    d = Drawing(460, 260)
    d.add(String(8, 244, "SysML BDD - Greenhouse System Structure", fontName="Helvetica-Bold", fontSize=10))

    for shape in _sysml_block(170, 190, 130, 28, "<<block>> GreenhouseSystem", colors.lightgrey):
        d.add(shape)

    blocks = [
        (20, 130, "SimulationClock"),
        (130, 130, "GreenhouseManager"),
        (250, 130, "EnvironmentPhysics"),
        (360, 130, "SoilModel"),
        (70, 70, "RuleBasedController"),
        (220, 70, "DashboardMetricsRuntime"),
        (350, 70, "DashboardActuatorsRuntime"),
    ]
    for x, y, name in blocks:
        for shape in _sysml_block(x, y, 95, 24, f"<<block>> {name}"):
            d.add(shape)
        d.add(Line(235, 190, x + 47, y + 24, strokeColor=colors.black))

    d.add(String(8, 18, "GreenhouseManager composes: AirState, SoilState, PlantState, OutdoorState", fontSize=8))
    return d


def make_ibd_diagram():
    d = Drawing(460, 260)
    d.add(String(8, 244, "SysML IBD - Internal Data and Control Flow", fontName="Helvetica-Bold", fontSize=10))

    for shape in _sysml_block(180, 180, 130, 28, "GreenhouseManager", colors.lightgrey):
        d.add(shape)
    for shape in _sysml_block(30, 180, 120, 24, "SimulationClock"):
        d.add(shape)
    for shape in _sysml_block(30, 130, 120, 24, "EnvironmentPhysics"):
        d.add(shape)
    for shape in _sysml_block(30, 80, 120, 24, "SoilModel"):
        d.add(shape)
    for shape in _sysml_block(330, 145, 120, 24, "DashboardMetrics"):
        d.add(shape)
    for shape in _sysml_block(330, 95, 120, 24, "DashboardActuators"):
        d.add(shape)
    for shape in _sysml_block(180, 80, 130, 24, "RuleBasedController"):
        d.add(shape)

    d.add(Line(150, 192, 180, 192))
    d.add(Line(150, 142, 180, 192))
    d.add(Line(150, 92, 180, 192))
    d.add(Line(245, 180, 245, 104))
    d.add(Line(310, 156, 330, 157))
    d.add(Line(310, 106, 330, 107))

    d.add(String(154, 196, "dt/time", fontSize=7))
    d.add(String(154, 146, "air/outdoor state", fontSize=7))
    d.add(String(154, 96, "soil state", fontSize=7))
    d.add(String(252, 135, "actuator decisions", fontSize=7))
    d.add(String(314, 160, "metric values", fontSize=7))
    d.add(String(314, 110, "status + control", fontSize=7))
    return d


def make_activity_diagram():
    d = Drawing(460, 300)
    d.add(String(8, 284, "SysML Activity - Simulation Tick", fontName="Helvetica-Bold", fontSize=10))

    steps = [
        (170, 240, "Start"),
        (130, 205, "Compute dt"),
        (130, 170, "Update Outdoor + Air"),
        (130, 135, "Update Soil + Plant"),
        (130, 100, "Evaluate Controller"),
        (130, 65, "Update UI / Logs"),
        (170, 25, "End"),
    ]
    for x, y, text in steps:
        fill = colors.lightgrey if text in ("Start", "End") else colors.whitesmoke
        for shape in _sysml_block(x, y, 160, 24, text, fill):
            d.add(shape)

    arrows = [(250, 240, 250, 229), (250, 205, 250, 194), (250, 170, 250, 159), (250, 135, 250, 124), (250, 100, 250, 89), (250, 65, 250, 49)]
    for x1, y1, x2, y2 in arrows:
        d.add(Line(x1, y1, x2, y2))
        d.add(Polygon([x2 - 3, y2 + 4, x2 + 3, y2 + 4, x2, y2], fillColor=colors.black, strokeColor=colors.black))
    return d


def make_state_diagram():
    d = Drawing(460, 260)
    d.add(String(8, 244, "SysML State Machine - Grow Light Controller (Concept)", fontName="Helvetica-Bold", fontSize=10))

    for shape in _sysml_block(50, 150, 140, 28, "State: LightOff", colors.whitesmoke):
        d.add(shape)
    for shape in _sysml_block(270, 150, 140, 28, "State: LightOn", colors.whitesmoke):
        d.add(shape)

    d.add(Line(190, 164, 270, 164))
    d.add(Polygon([264, 168, 264, 160, 270, 164], fillColor=colors.black, strokeColor=colors.black))
    d.add(String(198, 171, "lux < threshold_on", fontSize=8))

    d.add(Line(270, 156, 190, 156))
    d.add(Polygon([196, 160, 196, 152, 190, 156], fillColor=colors.black, strokeColor=colors.black))
    d.add(String(196, 142, "lux > threshold_off", fontSize=8))

    d.add(String(50, 112, "Hysteresis requirement: threshold_on < threshold_off", fontSize=8))
    d.add(String(50, 98, "Goal: avoid ON/OFF chatter around a single cutoff.", fontSize=8))
    return d


def build_pdf(output_path: str) -> None:
    doc = SimpleDocTemplate(
        output_path,
        pagesize=A4,
        rightMargin=2 * cm,
        leftMargin=2 * cm,
        topMargin=2 * cm,
        bottomMargin=2 * cm,
    )

    styles = getSampleStyleSheet()
    title_style = ParagraphStyle(
        "Title",
        parent=styles["Heading1"],
        fontSize=22,
        leading=28,
        spaceAfter=10,
        alignment=1,
    )
    subtitle_style = ParagraphStyle(
        "Subtitle",
        parent=styles["Normal"],
        fontSize=11,
        leading=14,
        alignment=1,
        textColor=colors.grey,
    )
    h_style = ParagraphStyle(
        "SectionHeading",
        parent=styles["Heading2"],
        fontSize=14,
        leading=18,
        spaceBefore=8,
        spaceAfter=6,
    )
    body_style = ParagraphStyle(
        "Body",
        parent=styles["Normal"],
        fontSize=11,
        leading=16,
        spaceAfter=6,
    )
    mono_style = ParagraphStyle(
        "Mono",
        parent=styles["Code"],
        fontName="Courier",
        fontSize=9,
        leading=12,
    )

    today = datetime.now().strftime("%d %B %Y")

    story = []

    # Title Page
    story.append(Spacer(1, 4 * cm))
    story.append(p("Greenhouse Digital Twin Project", title_style))
    story.append(p("First Progress Report", title_style))
    story.append(Spacer(1, 0.5 * cm))
    story.append(p("Course Submission Draft", subtitle_style))
    story.append(p(f"Date: {today}", subtitle_style))
    story.append(Spacer(1, 2 * cm))

    info_table = Table(
        [
            ["Project Theme", "Cyber-Physical Greenhouse Simulation in Unity"],
            ["Current Phase", "Minimum Working Prototype + UI Refactor"],
            ["Primary Toolchain", "Unity 6, C#, TextMeshPro"],
            ["Submission Type", "Progress Report (Phase-1 level)"],
        ],
        colWidths=[5 * cm, 10 * cm],
    )
    info_table.setStyle(
        TableStyle(
            [
                ("GRID", (0, 0), (-1, -1), 0.5, colors.grey),
                ("BACKGROUND", (0, 0), (0, -1), colors.whitesmoke),
                ("FONTNAME", (0, 0), (-1, -1), "Helvetica"),
                ("FONTSIZE", (0, 0), (-1, -1), 10),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 6),
                ("RIGHTPADDING", (0, 0), (-1, -1), 6),
                ("TOPPADDING", (0, 0), (-1, -1), 5),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
            ]
        )
    )
    story.append(info_table)
    story.append(PageBreak())

    # Introduction
    story.append(p("1. Introduction", h_style))
    story.append(
        p(
            "This report summarizes the first implementation stage of a greenhouse digital twin developed in Unity. "
            "The objective is to simulate key greenhouse dynamics (air temperature, humidity, soil moisture, light, and actuator behavior) "
            "within a structured cyber-physical architecture. "
            "The current milestone focuses on making the simulation loop stable, centralizing shared state management, "
            "and creating a configurable dashboard that can scale to additional parameters.",
            body_style,
        )
    )

    # Work Completed
    story.append(p("2. Work Completed", h_style))
    story.append(
        p(
            "<b>2.1 Core Simulation Orchestration</b><br/>"
            "A central orchestrator (<i>GreenhouseManager</i>) now coordinates all state updates in a fixed simulation loop. "
            "Air, soil, plant, outdoor, and actuator states are managed in one place and consumed by UI modules.",
            body_style,
        )
    )
    story.append(
        p(
            "<b>2.2 Physics and Time System</b><br/>"
            "<i>SimulationClock</i> supports variable simulation speed, while <i>EnvironmentPhysics</i> and <i>SoilModel</i> "
            "handle climate and soil evolution. Runtime tuning was performed to reduce unrealistic saturation and clipping behavior.",
            body_style,
        )
    )
    story.append(
        p(
            "<b>2.3 Rule-Based Control and Actuation</b><br/>"
            "A hysteresis-based <i>RuleBasedController</i> was integrated with actuator booleans "
            "(fan, heater, irrigation, mister, grow light). Crop profile dependency is active and required for control decisions.",
            body_style,
        )
    )
    story.append(
        p(
            "<b>2.4 Runtime Dashboard Refactor</b><br/>"
            "The old static text-based UI was replaced with a prefab-driven runtime dashboard. "
            "Metrics are instantiated from a selected list, and actuator rows are generated from one reusable actuator prefab, "
            "including switch interaction through a dedicated RadioSwitch script.",
            body_style,
        )
    )

    # SysML
    story.append(p("3. SysML Artifacts (Draft)", h_style))
    story.append(
        p(
            "The following diagrams are provided in textual form for this progress report draft. "
            "They follow SysML intent and naming consistency and will be redrawn as final visual diagrams before final submission.",
            body_style,
        )
    )

    story.append(make_bdd_diagram())
    story.append(Spacer(1, 0.3 * cm))
    story.append(make_ibd_diagram())
    story.append(PageBreak())
    story.append(make_activity_diagram())
    story.append(Spacer(1, 0.3 * cm))
    story.append(make_state_diagram())
    story.append(Spacer(1, 0.4 * cm))

    bdd = """\
SysML BDD (Block Definition Diagram)
------------------------------------
[GreenhouseSystem]
  +-- [SimulationClock]
  +-- [GreenhouseManager]
  +-- [EnvironmentPhysics]
  +-- [SoilModel]
  +-- [RuleBasedController]
  +-- [DashboardMetricsRuntime]
  +-- [DashboardActuatorsRuntime]

GreenhouseManager
  <<composes>> AirState, SoilState, PlantState, OutdoorState
  <<references>> SimulationClock, EnvironmentPhysics, SoilModel, RuleBasedController
"""
    story.append(Preformatted(bdd, mono_style))

    ibd = """\
SysML IBD (Internal Block Diagram) - Data Flow
----------------------------------------------
SimulationClock --> GreenhouseManager : dt, simTime, hourOfDay
EnvironmentPhysics --> GreenhouseManager.airState / outdoorState
SoilModel --> GreenhouseManager.soilState
RuleBasedController --> GreenhouseManager.actuatorFlags
GreenhouseManager --> DashboardMetricsRuntime : selected metric values
GreenhouseManager --> DashboardActuatorsRuntime : actuator status + command writes
"""
    story.append(Preformatted(ibd, mono_style))

    activity = """\
SysML Activity (Simulation Tick)
--------------------------------
[Start]
  -> Compute dt from SimulationClock
  -> Update outdoor dynamics
  -> Update indoor air dynamics
  -> Update soil dynamics
  -> Update plant and sensor models (if assigned)
  -> Evaluate rule-based control
  -> Update energy tracking (if assigned)
  -> Refresh dashboard items (runtime UI)
[End]
"""
    story.append(Preformatted(activity, mono_style))

    # Challenges
    story.append(p("4. Challenges", h_style))
    story.append(
        p(
            "<b>State Wiring Complexity:</b> During scene refactoring, component references were split across old and new roots, "
            "causing null-reference warnings and stale UI text outputs.",
            body_style,
        )
    )
    story.append(
        p(
            "<b>Control Instability at High Time Scale:</b> With aggressive time acceleration, some variables showed non-physical behavior "
            "or toggled rapidly (especially light-based actuation around threshold boundaries).",
            body_style,
        )
    )
    story.append(
        p(
            "<b>UI Scalability:</b> A static text layout did not scale with additional variables. "
            "This prompted migration to prefab-based runtime generation with configurable metric/actuator lists.",
            body_style,
        )
    )

    # Next Steps
    story.append(p("5. Next Steps", h_style))
    next_steps = [
        "Finalize metric and actuator layout constraints for consistent readability on 1080p and lower resolutions.",
        "Add explicit time formatting (HH:mm) for hour-of-day display and verify high-speed readability.",
        "Tune control thresholds and grow-light behavior to reduce chatter while preserving responsiveness.",
        "Integrate optional sensor-mode display (measured values vs true state) in the metric runtime.",
        "Complete formal SysML visual diagrams (BDD, IBD, Activity, and State Machine) for final submission.",
        "Prepare short validation scenarios (heat wave, irrigation stress, low-light condition) and capture evidence.",
    ]
    for i, item in enumerate(next_steps, start=1):
        story.append(p(f"{i}. {item}", body_style))

    # Conclusion
    story.append(p("6. Conclusion", h_style))
    story.append(
        p(
            "At this stage, the project has reached a stable and testable architecture with a centralized orchestrator, "
            "working simulation loop, rule-based control integration, and dynamic dashboard infrastructure. "
            "The system is now suitable for controlled tuning and structured validation. "
            "The upcoming phase will focus on model calibration, usability polish, and formally packaged SysML documentation.",
            body_style,
        )
    )

    doc.build(story)


if __name__ == "__main__":
    build_pdf("First_Progress_Report.pdf")
