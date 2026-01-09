#!/bin/bash

# Password Generator Script for Golf Application
# Generates secure passwords for initial user setup

echo "=========================================="
echo "Golf Application - Password Generator"
echo "=========================================="
echo ""
echo "This script generates secure passwords for:"
echo "- 1 Federation user"
echo "- 10 Club Captain users"
echo ""
echo "Generated passwords:"
echo ""

# Generate Federation password
FED_PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-14)
echo "Federation:"
echo "  Username: federation"
echo "  Password: $FED_PASSWORD"
echo ""

# Generate passwords for 10 clubs
echo "Club Captains:"
for i in {1..10}; do
    PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-14)
    echo "  Club $i:"
    echo "    Username: club${i}_captain"
    echo "    Password: $PASSWORD"
done

echo ""
echo "=========================================="
echo "CSV Format (for import):"
echo "=========================================="
echo "Username,Password,ClubCode,Role"
echo "federation,$FED_PASSWORD,FEDR,Federation"
for i in {1..10}; do
    PASSWORD=$(openssl rand -base64 16 | tr -d "=+/" | cut -c1-14)
    echo "club${i}_captain,$PASSWORD,CLB${i},ClubCaptain"
done

echo ""
echo "=========================================="
echo "⚠️  IMPORTANT: Save these passwords securely!"
echo "=========================================="
