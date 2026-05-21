import os
import re

def generate_unity_report(project_path, output_file):
    with open(output_file, 'w', encoding='utf-8') as out:
        out.write("=== 项目目录树 ===\n")
        # 生成目录树（如果系统没有 tree 命令，改用手动遍历）
        try:
            os.system(f'tree /F "{project_path}" > temp_tree.txt')
            with open('temp_tree.txt', 'r', encoding='utf-8') as f:
                out.write(f.read())
            os.remove('temp_tree.txt')
        except:
            out.write("(无法生成tree，跳过)\n")
            for root, dirs, files in os.walk(project_path):
                level = root.replace(project_path, '').count(os.sep)
                indent = ' ' * 2 * level
                out.write(f'{indent}{os.path.basename(root)}/\n')
                sub_indent = ' ' * 2 * (level+1)
                for file in files:
                    out.write(f'{sub_indent}{file}\n')
        
        out.write("\n\n=== C# 脚本内容 ===\n")
        for root, dirs, files in os.walk(project_path):
            # 跳过无关文件夹
            if any(skip in root for skip in ['Library', 'Temp', 'obj', 'Build', 'Packages', 'Logs']):
                continue
            for file in files:
                if file.endswith('.cs'):
                    full_path = os.path.join(root, file)
                    out.write(f"\n--- {full_path} ---\n")
                    try:
                        with open(full_path, 'r', encoding='utf-8') as cf:
                            out.write(cf.read())
                    except:
                        out.write("(读取失败)\n")
        
        out.write("\n\n=== 预制体(.prefab)和场景(.unity)摘要 ===\n")
        for root, dirs, files in os.walk(project_path):
            if any(skip in root for skip in ['Library', 'Temp']):
                continue
            for file in files:
                if file.endswith('.prefab') or file.endswith('.unity'):
                    full_path = os.path.join(root, file)
                    out.write(f"\n--- {full_path} ---\n")
                    # 提取可能的脚本引用（简单匹配）
                    scripts_found = []
                    try:
                        with open(full_path, 'r', encoding='utf-8') as pf:
                            content = pf.read(5000)  # 只读前5000字符，避免太大
                            # 查找 MonoBehaviour 的脚本 GUID
                            matches = re.findall(r'm_Script:.*?guid: ([a-f0-9]+)', content)
                            if matches:
                                scripts_found.append(f"找到 {len(set(matches))} 个脚本引用")
                            else:
                                scripts_found.append("未找到脚本引用（可能是二进制预制体）")
                    except:
                        scripts_found.append("无法读取")
                    out.write(f"信息：{', '.join(scripts_found)}\n")
        
        out.write("\n\n=== ScriptableObject (.asset) 文件列表 ===\n")
        for root, dirs, files in os.walk(project_path):
            if any(skip in root for skip in ['Library', 'Temp']):
                continue
            for file in files:
                if file.endswith('.asset'):
                    out.write(f"{os.path.join(root, file)}\n")
    
    print(f"报告已生成: {output_file}")

if __name__ == "__main__":
    # 请修改下面的路径为你的 Unity 项目根目录
    project_folder = r"C:\threeKingdomSlayer\threeKingdomSlayer"
    output_path = r"C:\threeKingdomSlayer\_summaries\unity_report.txt"
    
    generate_unity_report(project_folder, output_path)