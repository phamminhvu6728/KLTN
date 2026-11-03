from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT

ROOT = Path(__file__).resolve().parent
OUT = ROOT / 'Huong dan demo'
OUT.mkdir(exist_ok=True)

def base(title, intro):
    d=Document()
    for border in list(d.styles.element.xpath('.//w:pBdr')):
        border.getparent().remove(border)
    s=d.sections[0]
    s.page_width=Inches(8.5);s.page_height=Inches(11)
    s.top_margin=Inches(.7);s.bottom_margin=Inches(.65)
    s.left_margin=Inches(.8);s.right_margin=Inches(.8)
    for name in ['Normal','Title','Subtitle','Heading 1','Heading 2','Heading 3']:
        style=d.styles[name];style.font.name='Arial';style.font.color.rgb=RGBColor(0,0,0)
        style.element.get_or_add_rPr().rFonts.set(qn('w:eastAsia'),'Arial')
    normal=d.styles['Normal'];normal.font.size=Pt(11)
    normal.paragraph_format.line_spacing=1.12;normal.paragraph_format.space_after=Pt(6)
    for name,size in [('Title',25),('Heading 1',18),('Heading 2',12.5)]:
        style=d.styles[name];style.font.size=Pt(size)
        style.paragraph_format.space_before=Pt(12);style.paragraph_format.space_after=Pt(8)
        style.paragraph_format.keep_with_next=True
    d.styles['Title'].paragraph_format.space_before=Pt(0)
    d.core_properties.title=title;d.core_properties.author='';d.core_properties.subject='Hệ thống hỗ trợ khách du lịch tự túc'
    d.add_paragraph(title,'Title')
    d.add_paragraph('Hệ thống hỗ trợ khách du lịch tự túc | Ngày 03 tháng 11 năm 2026','Subtitle')
    d.add_paragraph(intro)
    footer=s.footer.paragraphs[0];footer.alignment=WD_ALIGN_PARAGRAPH.RIGHT
    r=footer.add_run('Trang ');r.font.size=Pt(9)
    fld=OxmlElement('w:fldSimple');fld.set(qn('w:instr'),'PAGE');footer._p.append(fld)
    return d

def p(d,text): d.add_paragraph(text)
def h(d,text): d.add_paragraph(text,'Heading 2')
def page(d,title):
    d.add_page_break();d.add_paragraph(title,'Heading 1')
def steps(d,items):
    for i,text in enumerate(items,1):
        para=d.add_paragraph();para.paragraph_format.left_indent=Inches(.16)
        para.paragraph_format.first_line_indent=Inches(-.16)
        para.add_run(f'{i}. ').bold=True;para.add_run(text)
def say(d,text):
    para=d.add_paragraph();para.add_run('Lời nói gợi ý  ').bold=True;para.add_run('“'+text+'”').italic=True
def code(d,text):
    for line in text.split('\n'):
        para=d.add_paragraph();para.paragraph_format.space_after=Pt(3)
        r=para.add_run(line);r.font.name='Consolas';r.font.size=Pt(9.5)
def table(d,heads,rows,widths):
    t=d.add_table(rows=1,cols=len(heads));t.alignment=WD_TABLE_ALIGNMENT.CENTER;t.autofit=False
    for c,w in zip(t.columns,widths):c.width=Inches(w)
    for c,w,txt in zip(t.rows[0].cells,widths,heads):c.width=Inches(w);c.text=txt
    for row in rows:
        cells=t.add_row().cells
        for c,w,txt in zip(cells,widths,row):c.width=Inches(w);c.text=str(txt)
    borders=OxmlElement('w:tblBorders')
    for edge in ['top','left','bottom','right','insideH','insideV']:
        el=OxmlElement('w:'+edge);el.set(qn('w:val'),'single');el.set(qn('w:sz'),'4');el.set(qn('w:color'),'D9D9D9');borders.append(el)
    t._tbl.tblPr.append(borders)
    for i,row in enumerate(t.rows):
        cant=OxmlElement('w:cantSplit');row._tr.get_or_add_trPr().append(cant)
        if i==0:row._tr.get_or_add_trPr().append(OxmlElement('w:tblHeader'))
        for j,c in enumerate(row.cells):
            c.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            tcpr=c._tc.get_or_add_tcPr();m=OxmlElement('w:tcMar')
            for side in ['top','left','bottom','right']:
                e=OxmlElement('w:'+side);e.set(qn('w:w'),'95');e.set(qn('w:type'),'dxa');m.append(e)
            tcpr.append(m)
            if i==0 or i%2==0:
                shade=OxmlElement('w:shd');shade.set(qn('w:fill'),'E8EDF2' if i==0 else 'F7F8FA');tcpr.append(shade)
            for para in c.paragraphs:
                para.paragraph_format.space_after=Pt(2);para.paragraph_format.line_spacing=1.05
                for r in para.runs:r.font.size=Pt(10);r.bold=i==0
    d.add_paragraph().paragraph_format.space_after=Pt(0)

def qa(d,q,a,detail=None):
    h(d,q);p(d,'Có thể trả lời: “'+a+'”')
    if detail:p(d,'Hiểu thêm: '+detail)

