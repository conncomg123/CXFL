#include "../include/CFile.h"
#include <iostream>
#include <fstream>
#include <filesystem>

namespace fs = std::filesystem;

//Read, write and remove work, but only for files, not 
//directories.

//getAudioFormat and getSamples are gonna suck, probably gonna
//need portAudio.

CFile::CFile(const std::string& filename) {
    this->filename = filename;
}

CFile::~CFile() {
}

bool CFile::exists(const std::string& filename) {
    return std::ifstream(filename.c_str()).good();
}

long CFile::getSize(const std::string& filename) {
    if (exists(filename)) {
        std::ifstream file(filename, std::ios::binary | std::ios::ate);
        return file.tellg();
    }
    return -1;
}

std::string CFile::read(const std::string& filename) {
    std::string content;

    if (exists(filename)) {
        std::ifstream file(filename);
        if (file.is_open()) {
            content.assign((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());
            file.close();
        }
    }

    return content;
}

bool CFile::remove(const std::string& targetPath) {
    if (exists(targetPath)) {
        try {
            std::remove(targetPath.c_str());
            return true;
        } catch (const std::exception& e) {
            std::cerr << "Error removing " << targetPath << ": " << e.what() << std::endl;
            return false;
        }
    }
    return false;
}

bool CFile::write(const std::string& filename, const std::string& content, const std::string& mode) {
    std::ofstream file(filename.c_str(), std::ios_base::out);

    if (file.is_open()) {
        file << content;
        file.close();
        return true;
    } else {
        std::cerr << "Error writing to " << filename << std::endl;
        return false;
    }
}