"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.create_excel_generated = create_excel_generated;
const ExcelJS = __importStar(require("exceljs"));
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
async function create_excel_generated(input) {
    const { filename, data, sessionKey } = input;
    // Determine output path
    const outputDir = process.env.STORAGE_ROOT || './generated_files';
    const sessionDir = sessionKey ? path.join(outputDir, sessionKey) : outputDir;
    if (!fs.existsSync(sessionDir)) {
        fs.mkdirSync(sessionDir, { recursive: true });
    }
    const filePath = path.join(sessionDir, filename);
    // Create workbook
    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet('Sheet1');
    // Add data
    if (Array.isArray(data)) {
        data.forEach(row => {
            if (Array.isArray(row)) {
                worksheet.addRow(row);
            }
            else if (typeof row === 'object') {
                worksheet.addRow(Object.values(row));
            }
        });
    }
    // Auto-fit columns
    worksheet.columns.forEach(column => {
        if (column) {
            let maxLength = 0;
            column.eachCell?.({ includeEmpty: true }, cell => {
                const cellValue = cell.value ? cell.value.toString() : '';
                maxLength = Math.max(maxLength, cellValue.length);
            });
            column.width = Math.min(maxLength + 2, 50);
        }
    });
    // Save file
    await workbook.xlsx.writeFile(filePath);
    return {
        success: true,
        file_path: filePath,
        file_name: filename,
        message: `Excel file created successfully using exceljs: ${filename}`
    };
}
