import subprocess
import datetime
import os

# Your Supabase Postgres connection string
DB_URL = os.getenv("SUPABASE_DB_URL")

if not DB_URL:
    raise Exception("Set SUPABASE_DB_URL environment variable")

# Output file name with timestamp
timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
output_file = f"supabase_backup_{timestamp}.sql"

# Run pg_dump
cmd = [
    "pg_dump",
    DB_URL,
    "-f",
    output_file,
    "--no-owner",
    "--clean",
    "--if-exists"
]

print("Starting Supabase database dump...")
subprocess.run(cmd, check=True)

print(f"Backup complete: {output_file}")