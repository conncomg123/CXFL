#include "../include/XFLDocument.h"
#include <stdexcept>
void XFLDocument::loadTimelines(pugi::xml_node& root) {
	auto timelines = root.child("timelines").children("DOMTimeline");
	for (auto iter = timelines.begin(); iter != timelines.end(); ++iter) {
		this->timelines.push_back(std::make_unique<Timeline>(*iter));
	}
}
XFLDocument::XFLDocument(const std::string& filename) {
	this->filename = filename;
	xflTree = std::make_unique<pugi::xml_document>();
	auto result = xflTree->load_file(filename.c_str());
	if (!result) {
		throw std::runtime_error("Failed to load XFL file: " + std::string(result.description()));
	}
	this->root = xflTree->document_element();
	loadTimelines(this->root);
}
XFLDocument::~XFLDocument() {

}
void XFLDocument::write(const std::string& filename) {
	xflTree->save_file(filename.c_str());
}
Timeline* XFLDocument::getTimeline(int index) {
	return timelines[index].get();
}
pugi::xml_node& XFLDocument::getRoot() {
	return this->root;
}