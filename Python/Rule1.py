import pandas as pd

# Load Excel file
file_path = r'C:\Users\ARohilla\Tech Titans\Data Rule 1.xlsx'  # Replace with your actual file path
xls = pd.ExcelFile(file_path)

# Read data from sheets
df1 = pd.read_excel(xls, sheet_name='Rule1 Source1')
df2 = pd.read_excel(xls, sheet_name='Rule1 Source2')

# Define key columns for comparison
key_columns = ['Business_date', 'Reporting_id', 'CC', 'GL', 'PC']

# Merge both datasets on common keys
merged_df = pd.merge(df1, df2, on=key_columns, how='outer', suffixes=('_Src1', '_Src2'))

# Calculate balance difference
merged_df['Balance_Difference'] = merged_df['BalanceGBP_Src1'].fillna(0) - merged_df['BalanceGBP_Src2'].fillna(0)

# Add comments based on conditions
def classify_difference(row):
    if pd.isna(row['BalanceGBP_Src1']):
        return 'Src1 Balance Missing'
    elif pd.isna(row['BalanceGBP_Src2']):
        return 'Src2 Balance Missing'
    elif row['Balance_Difference'] == 0:
        return 'Match'
    else:
        return 'Mismatch'

merged_df['Comments'] = merged_df.apply(classify_difference, axis=1)

# Select required columns for output
output_columns = key_columns + ['BalanceGBP_Src1', 'BalanceGBP_Src2', 'Balance_Difference', 'Comments']
final_df = merged_df[output_columns]

# Export results to a new Excel file
output_file = 'comparison_results.xlsx'
final_df.to_excel(output_file, index=False)

print(f"Comparison results saved to {output_file}")