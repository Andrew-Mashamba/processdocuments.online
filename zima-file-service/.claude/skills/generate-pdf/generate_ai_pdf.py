import sys
import json
from reportlab.lib.pagesizes import letter
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib import colors
from reportlab.lib.units import inch

def create_ai_pdf(output_path):
    # Create the PDF document
    doc = SimpleDocTemplate(output_path, pagesize=letter, rightMargin=72, leftMargin=72, topMargin=72, bottomMargin=18)

    # Styles
    styles = getSampleStyleSheet()
    title_style = styles['Title']
    heading_style = styles['Heading2']
    normal_style = styles['Normal']

    # Content
    story = []

    # Title
    story.append(Paragraph("Artificial Intelligence: A Comprehensive Overview", title_style))
    story.append(Spacer(1, 12))

    # 1. Definition
    story.append(Paragraph("1. What is Artificial Intelligence?", heading_style))
    story.append(Paragraph("Artificial Intelligence (AI) is a transformative field of computer science that enables machines to simulate human intelligence. It encompasses the development of computer systems capable of performing tasks that typically require human cognitive abilities such as learning, reasoning, problem-solving, perception, and language understanding.", normal_style))
    story.append(Spacer(1, 12))

    # 2. Types of AI
    story.append(Paragraph("2. Types of Artificial Intelligence", heading_style))
    ai_types_data = [
        ['Type', 'Description', 'Characteristics'],
        ['Narrow AI (Weak AI)', 'Focused on specific tasks', 'Limited to predefined functions'],
        ['General AI (Strong AI)', 'Human-like intelligence across domains', 'Hypothetical, not yet achieved'],
        ['Artificial Super Intelligence (ASI)', 'Surpasses human intelligence', 'Theoretical future concept']
    ]
    ai_types_table = Table(ai_types_data, colWidths=[1.5*inch, 2.5*inch, 2*inch])
    ai_types_table.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,0), colors.grey),
        ('TEXTCOLOR', (0,0), (-1,0), colors.whitesmoke),
        ('ALIGN', (0,0), (-1,-1), 'CENTER'),
        ('FONTNAME', (0,0), (-1,0), 'Helvetica-Bold'),
        ('FONTSIZE', (0,0), (-1,0), 12),
        ('BOTTOMPADDING', (0,0), (-1,0), 12),
        ('BACKGROUND', (0,1), (-1,-1), colors.beige),
        ('GRID', (0,0), (-1,-1), 1, colors.black)
    ]))
    story.append(ai_types_table)
    story.append(Spacer(1, 12))

    # 3. Key Technologies
    story.append(Paragraph("3. Key AI Technologies", heading_style))
    key_tech_data = [
        ['Technology', 'Description', 'Key Applications'],
        ['Machine Learning', 'Algorithms that improve through experience', 'Predictive analytics, recommendation systems'],
        ['Deep Learning', 'Neural networks mimicking brain structure', 'Image recognition, natural language processing'],
        ['Natural Language Processing', 'Understanding human language', 'Chatbots, translation services'],
        ['Computer Vision', 'Interpreting visual information', 'Facial recognition, medical imaging']
    ]
    key_tech_table = Table(key_tech_data, colWidths=[1.5*inch, 2.5*inch, 2*inch])
    key_tech_table.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,0), colors.darkblue),
        ('TEXTCOLOR', (0,0), (-1,0), colors.whitesmoke),
        ('ALIGN', (0,0), (-1,-1), 'CENTER'),
        ('FONTNAME', (0,0), (-1,0), 'Helvetica-Bold'),
        ('FONTSIZE', (0,0), (-1,0), 12),
        ('BOTTOMPADDING', (0,0), (-1,0), 12),
        ('BACKGROUND', (0,1), (-1,-1), colors.lightblue),
        ('GRID', (0,0), (-1,-1), 1, colors.black)
    ]))
    story.append(key_tech_table)
    story.append(Spacer(1, 12))

    # 4. Real-World Applications
    story.append(Paragraph("4. Real-World AI Applications", heading_style))
    applications_data = [
        ['Industry', 'AI Applications', 'Impact'],
        ['Healthcare', 'Disease diagnosis, medical imaging', 'Improved diagnostic accuracy'],
        ['Finance', 'Fraud detection, algorithmic trading', 'Enhanced risk management'],
        ['Transportation', 'Autonomous vehicles, traffic optimization', 'Increased safety and efficiency'],
        ['Education', 'Personalized learning systems', 'Adaptive learning experiences']
    ]
    applications_table = Table(applications_data, colWidths=[1.5*inch, 2.5*inch, 2*inch])
    applications_table.setStyle(TableStyle([
        ('BACKGROUND', (0,0), (-1,0), colors.darkgreen),
        ('TEXTCOLOR', (0,0), (-1,0), colors.whitesmoke),
        ('ALIGN', (0,0), (-1,-1), 'CENTER'),
        ('FONTNAME', (0,0), (-1,0), 'Helvetica-Bold'),
        ('FONTSIZE', (0,0), (-1,0), 12),
        ('BOTTOMPADDING', (0,0), (-1,0), 12),
        ('BACKGROUND', (0,1), (-1,-1), colors.lightgreen),
        ('GRID', (0,0), (-1,-1), 1, colors.black)
    ]))
    story.append(applications_table)
    story.append(Spacer(1, 12))

    # 5. Ethical Considerations
    story.append(Paragraph("5. Ethical Considerations in AI", heading_style))
    story.append(Paragraph("As AI continues to advance, several critical ethical considerations have emerged:", normal_style))
    ethical_points = [
        "Privacy and data protection",
        "Algorithmic bias and fairness",
        "Transparency of AI decision-making",
        "Potential job market disruption",
        "Responsible development and use of AI technologies"
    ]
    for point in ethical_points:
        story.append(Paragraph(f"• {point}", normal_style))

    # Build PDF
    doc.build(story)
    print(f"PDF generated successfully at {output_path}")

if __name__ == "__main__":
    output_path = sys.argv[1] if len(sys.argv) > 1 else "/Volumes/DATA/QWEN/zima-file-service/generated_files/Artificial_Intelligence_Overview.pdf"
    create_ai_pdf(output_path)