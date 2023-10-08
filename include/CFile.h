#ifndef CFILE_H
#define CFILE_H

#include <string>
#include <fstream>
#include <iostream>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <filesystem>

class CFile {
    protected:
    public:
        CFile(const std::string& filename);
        ~CFile();
        bool exists(const std::string& filename);
        std::string getAudioFormat(const std::string& filename);
        long getSize(const std::string& filename);
        int getSamples(const std::string& filename);
        std::string read(const std::string& filename);
        bool remove(const std::string& targetPath);
        bool write(const std::string& filename, const std::string& content, const std::string& mode = "w");
    private:
        std::string filename;
};

#endif // CFILE_H