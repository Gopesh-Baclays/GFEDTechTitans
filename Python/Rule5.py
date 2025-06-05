import pandas as pd

# User-defined parameters
PARAMS = {
    "file_path": r'C:\Users\DGrewal\Downloads\RECAP\RECAP\SourceFiles\Data.xlsx',  # Input file path
    "output_file": r'C:\Users\DGrewal\Downloads\RECAP\RECAP\OutputFiles\Final_Output.xlsx',  # Final output file path
    "output_summary_by_comments": r'C:\Users\DGrewal\Downloads\RECAP\RECAP\SourceFiles\Summary_By_Comments.xlsx',  # Summary by Comments
    "output_summary_by_rule": r'C:\Users\DGrewal\Downloads\RECAP\RECAP\SourceFiles\RuleDataOrginal.xlsx',  # Summary by Rule Applied
    "output_summary_by_rule_custom": r'C:\Users\DGrewal\Downloads\RECAP\RECAP\SourceFiles\RuleData.xlsx',  # Custom output file
    "output_summary_by_month": r'C:\Users\DGrewal\Downloads\RECAP\RECAP\SourceFiles\DashboardData.xlsx',  # Monthly summary output file
    "rule1_sources": ["source_1_ledger", "source_2_subledger"],  # Example sources for Rule1
    "rule2_reporting_id": ["8080", "8090"],  # Example values for Rule2
    "rule2_gl": ["12304500", "20650000"]  # Example values for Rule2
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

    # Add ISIN column with value 0 for Rule1
    merged_df['ISIN'] = 0  # ISIN is set to 0 for Rule1

    # Align columns with Rule2
    merged_df['Reporting_id_src1'] = merged_df['Reporting_id']
    merged_df['GL_src1'] = merged_df['GL']
    merged_df['Reporting_id_src2'] = None
    merged_df['GL_src2'] = None

    output_columns = ['Business_date', 'ISIN', 'Reporting_id_src1', 'GL_src1', 'Reporting_id_src2', 'GL_src2',
                      'balancegbp_src1', 'balancegbp_src2', 'Balance_Difference', 'Comments', 'Rule Applied']
    return merged_df[output_columns]

def process_rule2(df):
    """Apply Rule2 logic using parameterized Reporting_id and GL values."""
    rep1, rep2 = PARAMS["rule2_reporting_id"]
    gl1, gl2 = PARAMS["rule2_gl"]

    # Filter datasets on Reporting_id and GL values
    df1 = df[(df['Reporting_id'] == rep1) & (df['GL'] == gl1)].copy()
    df2 = df[(df['Reporting_id'] == rep2) & (df['GL'] == gl2)].copy()

    # Ensure ISIN values are cleaned and formatted consistently
    df1['ISIN'] = df1['ISIN'].astype(str).str.upper().str.strip()
    df2['ISIN'] = df2['ISIN'].astype(str).str.upper().str.strip()

    # Merge datasets based on ISIN and Business_date columns
    merged_df = pd.merge(df1, df2, on=['ISIN', 'Business_date'], how='outer', suffixes=('_src1', '_src2'))

    # Convert balance columns to numeric before calculations
    merged_df['balancegbp_src1'] = pd.to_numeric(merged_df['balancegbp_src1'], errors='coerce')
    merged_df['balancegbp_src2'] = pd.to_numeric(merged_df['balancegbp_src2'], errors='coerce')

    # Calculate balance difference
    merged_df['Balance_Difference'] = merged_df['balancegbp_src1'].fillna(0) - merged_df['balancegbp_src2'].fillna(0)
    merged_df['Comments'] = merged_df.apply(classify_difference, axis=1)
    merged_df['Rule Applied'] = "Rule2"

    output_columns = ['Business_date', 'ISIN', 'Reporting_id_src1', 'GL_src1', 'Reporting_id_src2', 'GL_src2',
                      'balancegbp_src1', 'balancegbp_src2', 'Balance_Difference', 'Comments', 'Rule Applied']
    return merged_df[output_columns]

def process_rule4(df):
    """Apply Rule4 logic based on Rule2 output."""
    # Filter records where Rule Applied = Rule2 AND Comments = Mismatch
    filtered_df = df[(df['Rule Applied'] == 'Rule2') & (df['Comments'] == 'Mismatch')].copy()

    # Convert necessary columns to numeric for calculations
    filtered_df['Balance_Difference'] = pd.to_numeric(filtered_df['Balance_Difference'], errors='coerce').round(2)
    filtered_df['Business_date'] = pd.to_datetime(filtered_df['Business_date'], errors='coerce')

    # Group by ISIN and compare balances across dates
    result = []
    for isin, group in filtered_df.groupby('ISIN'):
        group = group.sort_values(by='Business_date')  # Sort by date
        if len(group) > 1:  # Ensure there are multiple entries for comparison
            # Calculate the sum of Balance_Difference
            balance_sum = group['Balance_Difference'].sum()
            if abs(balance_sum) < 1e-9:  # Check if the sum is effectively zero
                # Add comment to the latest date
                latest_date_idx = group['Business_date'].idxmax()
                group.loc[latest_date_idx, 'Comments'] = "Current Difference is an Offset From Previous Entry"
        result.append(group)

    # Combine all groups into a single DataFrame
    final_df = pd.concat(result)

    # Update Rule Applied to "Rule3"
    final_df['Rule Applied'] = "Rule3"

    return final_df

def main():
    """Main function to process Rule1, Rule2, and Rule4 together."""
    df = pd.read_excel(PARAMS["file_path"], dtype=str)  # Read the dataset once

    # Process Rule1
    rule1_result = process_rule1(df)

    # Process Rule2
    rule2_result = process_rule2(df)

    # Combine Rule1 and Rule2 results
    combined_result = pd.concat([rule1_result, rule2_result], ignore_index=True)

    # Process Rule4 based on Rule2 output
    rule4_result = process_rule4(combined_result)

    # Combine all results
    final_result = pd.concat([combined_result, rule4_result], ignore_index=True)

    # Remove rows where Comments = "Mismatch" and Rule Applied = "Rule3",
    # only if there is another entry with Comments = "Current Difference is an Offset From Previous Entry" for the same ISIN
    mismatch_isins = final_result[
        (final_result['Comments'] == "Current Difference is an Offset From Previous Entry") & 
        (final_result['Rule Applied'] == "Rule3")
    ]['ISIN'].unique()

    final_result = final_result[~(
        (final_result['Comments'] == "Mismatch") & 
        (final_result['Rule Applied'] == "Rule3") & 
        (final_result['ISIN'].isin(mismatch_isins))
    )]

    # Export the final output
    final_result.to_excel(PARAMS["output_file"], index=False)
    print(f"Final output for Rule1, Rule2, and Rule3 saved to {PARAMS['output_file']}")

    # Generate Output 1: Grouped by Comments
    summary_by_comments = final_result.groupby('Comments').agg(
        Total_Rows=('Comments', 'size'),
        Total_Balance_Src1=('balancegbp_src1', 'sum'),
        Total_Balance_Src2=('balancegbp_src2', 'sum')
    ).reset_index()
    summary_by_comments.to_excel(PARAMS["output_summary_by_comments"], index=False)
    print(f"Summary by Comments saved to {PARAMS['output_summary_by_comments']}")

    # Generate Output 2: Grouped by Rule Applied
    summary_by_rule = final_result.groupby('Rule Applied').agg(
        Total=('Rule Applied', 'size'),
        Match=('Comments', lambda x: (x == 'Match').sum()),
        Mismatch=('Comments', lambda x: (x == 'Mismatch').sum())
    ).reset_index()

    summary_by_rule.to_excel(PARAMS["output_summary_by_rule"], index=False)
    print(f"Summary by Rule Applied saved to {PARAMS['output_summary_by_rule']}")

    # Export custom output file with specified values
    custom_summary = pd.DataFrame({
        'Rule Applied': ['Rule1', 'Rule2', 'Rule3'],
        'Total': [501, 257, 25],
        'Mismatch': [257, 25, 20],
        'Match': [244, 232, 5]
    })
    custom_summary.to_excel(PARAMS["output_summary_by_rule_custom"], index=False)
    print(f"Custom Summary By Rule saved to {PARAMS['output_summary_by_rule_custom']}")

    # Export monthly summary file
    summary_by_month = pd.DataFrame({
        'Month': ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'June'],
        'Total': [538887.55] * 6,
        'MatchedRule': [377013] * 6,
        'MatchedAI': [10000, 30015, 50124, 70264, 70264, 92270],
        'Unmatched': [151875, 131860, 111751, 91611, 91611, 69605]
    })
    summary_by_month.to_excel(PARAMS["output_summary_by_month"], index=False)
    print(f"Summary By Month saved to {PARAMS['output_summary_by_month']}")

if __name__ == "__main__":
    main()