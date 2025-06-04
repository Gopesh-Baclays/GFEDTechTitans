import pandas as pd

# User-defined parameters
PARAMS = {
    "file_path": r'C:\Users\ARohilla\Tech Titans\Data.xlsx',  # Update the correct path
    "rule_selection": "Rule1",  # Change to "Rule1" or "Rule2" as needed
    "rule1_sources": ["source_1_ledger", "source_2_subledger"],  # Example sources for Rule1
    "rule2_reporting_id": ["8080", "8090"],  # Example values for Rule2
    "rule2_gl": ["12304500", "20650000"],  # Example values for Rule2
    "output_file": "Processed_Data.xlsx"
}

def classify_difference(row):
    """Classify balance differences into meaningful comments."""
    if pd.isna(row['balancegbp_src1']) or row['balancegbp_src1'] == '':
        return 'Src1 Balance Missing'
    elif pd.isna(row['balancegbp_src2']) or row['balancegbp_src2'] == '':
        return 'Src2 Balance Missing'
    elif abs(row['Balance_Difference']) < 1e-9:  # Handling floating-point precision
        return 'Match'
    else:
        return 'Mismatch'

def process_rule1(df):
    """Apply Rule1 logic using parameterized sources."""
    source1, source2 = PARAMS["rule1_sources"]

    df1 = df[df['sources'] == source1].copy()
    df2 = df[df['sources'] == source2].copy()

    key_columns = ['Business_date', 'Reporting_id', 'CC', 'GL', 'PC']
    merged_df = pd.merge(df1, df2, on=key_columns, how='outer', suffixes=('_src1', '_src2'))

    # Preserve NaN values for missing balances before conversion
    merged_df['balancegbp_src1'] = pd.to_numeric(merged_df['balancegbp_src1'], errors='coerce')
    merged_df['balancegbp_src2'] = pd.to_numeric(merged_df['balancegbp_src2'], errors='coerce')

    merged_df['Balance_Difference'] = merged_df['balancegbp_src1'].fillna(0) - merged_df['balancegbp_src2'].fillna(0)
    merged_df['Comments'] = merged_df.apply(classify_difference, axis=1)
    merged_df['Rule Applied'] = "Rule1"

    output_columns = key_columns + ['balancegbp_src1', 'balancegbp_src2', 'Balance_Difference', 'Comments', 'Rule Applied']
    return merged_df[output_columns]

def process_rule2(df):
    """Apply Rule2 logic using parameterized Reporting_id and GL values."""
    rep1, rep2 = PARAMS["rule2_reporting_id"]
    gl1, gl2 = PARAMS["rule2_gl"]

    # Validate Reporting_id and GL values
    if rep1 not in df['Reporting_id'].unique() or rep2 not in df['Reporting_id'].unique():
        print(f"Error: Reporting_id values {rep1} or {rep2} not found in the dataset.")
        return pd.DataFrame()  # Return empty DataFrame

    if gl1 not in df['GL'].unique() or gl2 not in df['GL'].unique():
        print(f"Error: GL values {gl1} or {gl2} not found in the dataset.")
        return pd.DataFrame()  # Return empty DataFrame

    # Filter datasets on Reporting_id and GL values
    df1 = df[(df['Reporting_id'] == rep1) & (df['GL'] == gl1)].copy()
    df2 = df[(df['Reporting_id'] == rep2) & (df['GL'] == gl2)].copy()

    # Debugging Step: Print dataset sizes after filtering
    print("Rule2 Dataset 1 Size:", len(df1))
    print("Rule2 Dataset 2 Size:", len(df2))

    if len(df1) == 0 or len(df2) == 0:
        print("Warning: One or both filtered datasets are empty! Check Reporting_id and GL values.")
        return pd.DataFrame()  # Return empty DataFrame

    # Ensure ISIN values are cleaned and formatted consistently
    df1['ISIN'] = df1['ISIN'].astype(str).str.upper().str.strip()
    df2['ISIN'] = df2['ISIN'].astype(str).str.upper().str.strip()

    # Debugging Step: Print unique ISINs
    print("Unique ISINs in Dataset 1:", df1['ISIN'].unique())
    print("Unique ISINs in Dataset 2:", df2['ISIN'].unique())

    # Check if ISIN column exists and has matching values
    if 'ISIN' not in df1.columns or 'ISIN' not in df2.columns:
        print("Error: ISIN column missing in one or both datasets.")
        return pd.DataFrame()  # Return empty DataFrame

    # Merge datasets based on ISIN column
    merged_df = pd.merge(df1, df2, on='ISIN', how='outer', suffixes=('_src1', '_src2'))

    # Debugging Step: Print merged dataset size
    print("Merged Dataset Size (Rule2):", len(merged_df))
    if merged_df.empty:
        print("Warning: Merged dataset is empty. Check ISIN values.")
        return pd.DataFrame()  # Return empty DataFrame

    # Convert balance columns to numeric before calculations
    merged_df['balancegbp_src1'] = pd.to_numeric(merged_df['balancegbp_src1'], errors='coerce')
    merged_df['balancegbp_src2'] = pd.to_numeric(merged_df['balancegbp_src2'], errors='coerce')

    # Calculate balance difference
    merged_df['Balance_Difference'] = merged_df['balancegbp_src1'].fillna(0) - merged_df['balancegbp_src2'].fillna(0)
    merged_df['Comments'] = merged_df.apply(classify_difference, axis=1)
    merged_df['Rule Applied'] = "Rule2"

    export_columns = ['ISIN', 'Reporting_id_src1', 'GL_src1', 'Reporting_id_src2', 'GL_src2',
                      'balancegbp_src1', 'balancegbp_src2', 'Balance_Difference', 'Comments', 'Rule Applied']
    return merged_df[export_columns]

def main():
    """Main function to run the rule-based processing."""
    df = pd.read_excel(PARAMS["file_path"], dtype=str)  # Read the dataset once

    if PARAMS["rule_selection"] == "Rule1":
        result = process_rule1(df)
    elif PARAMS["rule_selection"] == "Rule2":
        result = process_rule2(df)
    else:
        print("Invalid rule selection.")
        return

    if not result.empty:
        result.to_excel(PARAMS["output_file"], index=False)
        print(f"Comparison results saved to {PARAMS['output_file']}")
    else:
        print("No data to export.")

if __name__ == "__main__":
    main()