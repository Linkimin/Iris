import fs from 'fs';
import path from 'path';

const skillsDir = '.opencode/skills';
const names = fs.readdirSync(skillsDir).filter(d => {
  const stat = fs.statSync(path.join(skillsDir, d));
  return stat.isDirectory() && fs.existsSync(path.join(skillsDir, d, 'SKILL.md'));
});

for (const name of names.sort()) {
  const filePath = path.join(skillsDir, name, 'SKILL.md');
  const content = fs.readFileSync(filePath, 'utf8');

  // Check for BOM
  const hasBOM = content.charCodeAt(0) === 0xFEFF;
  
  // Check first line
  const firstLine = content.split('\n')[0];
  const startsWithDash = firstLine === '---';
  
  // Check frontmatter parsing
  const match = content.match(/^---\r?\n([\s\S]*?)\r?\n---/);
  
  // Check empty lines
  const frontmatterEnd = content.indexOf('\n---\n') > -1 ? content.indexOf('\n---\n') : content.indexOf('\r\n---\r\n');
  const afterFrontmatter = frontmatterEnd > -1 ? content.substring(frontmatterEnd + 5, frontmatterEnd + 50).replace(/\r/g, '\\r').replace(/\n/g, '\\n') : 'N/A';
  
  console.log(`${name}:`);
  console.log(`  BOM: ${hasBOM}, firstLine="${firstLine}"`);
  console.log(`  frontmatter match: ${match ? 'YES' : 'NO'}`);
  if (match) {
    console.log(`  name field: "${match[1].match(/name:\s*(.+)/)?.[1] || 'NOT FOUND'}"`);
    console.log(`  compatibility: "${match[1].match(/compatibility:\s*(.+)/)?.[1] || 'NOT FOUND'}"`);
    console.log(`  description length: ${(match[1].match(/description:\s*(.+)/)?.[1] || '').length}`);
  }
  console.log(`  after frontmatter (first 50 chars): "${afterFrontmatter}"`);
}
