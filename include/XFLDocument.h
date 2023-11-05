#ifndef XFLDOCUMENT_H
#define XFLDOCUMENT_H
#include "../lib/minizip/mz.h"
#include "../lib/minizip/mz_os.h"
#include "../lib/minizip/mz_strm.h"
#include "../lib/minizip/mz_strm_buf.h"
#include "../lib/minizip/mz_strm_split.h"
#include "../lib/minizip/mz_zip.h"
#include "../lib/minizip/mz_zip_rw.h"
#include "pugixml.hpp"
#include "SymbolInstance.h"
#include <memory>
#include <vector>
#include "Timeline.h"
class XFLDocument {
private:
	std::string filename;
	pugi::xml_document doc;
	std::unique_ptr<pugi::xml_document> xflTree;
	std::vector<std::unique_ptr<Timeline>> timelines;
	void loadTimelines(pugi::xml_node& root);
	pugi::xml_node root;
	void load_xfl(const std::string& filename);
	void load_fla(const std::string& filename);
	void save_xfl(const std::string& filename);
	void save_fla(const std::string& filename);
public:
	XFLDocument(const std::string& filename);
	~XFLDocument();
	void write(const std::string& filename);
	SymbolInstance getSymbolInstance(const std::string& name);
	Timeline* getTimeline(unsigned int index);
	pugi::xml_node& getRoot();
};

#endif // XFLDOCUMENT_H