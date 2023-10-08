#include "../include/SymbolInstance.h"
SymbolInstance::SymbolInstance(pugi::xml_node& elementNode) : Element(elementNode),
Matrix(elementNode.child("matrix").child("Matrix")),
Point(elementNode.child("transformationPoint").child("Point")) {
	this->libraryItemName = elementNode.attribute("libraryItemName").as_string();
	this->selected = elementNode.attribute("isSelected").as_bool();
	this->symbolType = elementNode.attribute("symbolType").as_string();
	this->firstFrame = elementNode.attribute("firstFrame").as_uint();
	this->lastFrame = elementNode.attribute("lastFrame").as_uint();
	this->loop = elementNode.attribute("loop").as_string();
}
SymbolInstance::~SymbolInstance() {
}
SymbolInstance::SymbolInstance(SymbolInstance& symbolInstance) : Element(symbolInstance),
Matrix(*this->getMatrix()),
Point(*this->getPoint()) {
	this->setHeight(symbolInstance.getHeight());
	this->setWidth(symbolInstance.getWidth());
	this->setFirstFrame(symbolInstance.getFirstFrame());
	this->setLastFrame(symbolInstance.getLastFrame());
	this->setLibraryItemName(symbolInstance.getLibraryItemName());
	this->setLoop(symbolInstance.getLoop());
}
std::string SymbolInstance::getLibraryItemName() {
	return this->libraryItemName;
}
void SymbolInstance::setLibraryItemName(const std::string& libraryItemName) {
	this->Element::root.attribute("libraryItemName").set_value(libraryItemName.c_str());
	this->libraryItemName = libraryItemName;
}
bool SymbolInstance::isSelected() {
	return this->selected;
}
void SymbolInstance::setSelected(bool selected) {
	if(!selected) this->Element::root.remove_attribute("isSelected");
	else this->Element::root.attribute("isSelected").set_value(selected);
	this->selected = selected;
}
std::string SymbolInstance::getSymbolType() {
	return this->symbolType;
}
void SymbolInstance::setSymbolType(const std::string& symbolType) {
	this->Element::root.attribute("symbolType").set_value(symbolType.c_str());
	this->symbolType = symbolType;
}
unsigned int SymbolInstance::getFirstFrame() {
	return this->firstFrame;
}
void SymbolInstance::setFirstFrame(unsigned int firstFrame) {
	if(firstFrame == 0) this->Element::root.remove_attribute("firstFrame");
	else this->Element::root.attribute("firstFrame").set_value(firstFrame);
	this->firstFrame = firstFrame;
}
std::optional<unsigned int> SymbolInstance::getLastFrame() {
	return this->lastFrame;
}
void SymbolInstance::setLastFrame(std::optional<unsigned int> lastFrame) {
	if(!lastFrame) this->Element::root.remove_attribute("lastFrame");
	else this->Element::root.attribute("lastFrame").set_value(lastFrame.value());
	this->lastFrame = lastFrame;
}
std::string SymbolInstance::getLoop() {
	return this->loop;
}
void SymbolInstance::setLoop(const std::string& loop) {
	this->Element::root.attribute("loop").set_value(loop.c_str());
	this->loop = loop;
}
double SymbolInstance::getWidth() const {
	// if width is not UNDEF, return it
	if (std::abs(this->width - UNDEF) < std::numeric_limits<double>::epsilon()) return this->width;
	// else, use private implementation and then set it (todo)
	return 0;
}
double SymbolInstance::getHeight() const {
	// if height is not UNDEF, return it
	if (std::abs(this->height - UNDEF) < std::numeric_limits<double>::epsilon()) return this->height;
	// else, use private implementation and then set it (todo)
	return 0;
}
Matrix* SymbolInstance::getMatrix() {
	return static_cast<Matrix*>(this);
}
Point* SymbolInstance::getPoint() {
	return static_cast<Point*>(this);
}

double SymbolInstance::getWidthRecur() const {
	// TODO: use a recursive function to go inside symbol, get the smallest width that contains all elements on the current frame, and return it
	return UNDEF;
}
double SymbolInstance::getHeightRecur() const {
	return UNDEF;
}