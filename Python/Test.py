folder_path = r'C:\Users\DGrewal\Downloads\RECAP\RECAP\Python'
file_name = 'newfilee.txt'
full_path = f"{folder_path}\\{file_name}"

with open(full_path, 'w') as f:
    f.write("This is a new text file.")

print(f"File created at: {full_path}")