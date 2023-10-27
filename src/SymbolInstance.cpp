#include "../include/SymbolInstance.h"
#include <limits>
SymbolInstance::SymbolInstance(pugi::xml_node& elementNode) noexcept : Instance(elementNode) {
	this->selected = elementNode.attribute("isSelected").as_bool();
	this->symbolType = elementNode.attribute("symbolType").as_string();
	this->firstFrame = elementNode.attribute("firstFrame").as_uint();
	this->lastFrame = elementNode.attribute("lastFrame").empty() ? std::nullopt : std::make_optional<unsigned int>(elementNode.attribute("lastFrame").as_uint());
	this->loop = elementNode.attribute("loop").as_string();
}
SymbolInstance::~SymbolInstance() noexcept {
}
SymbolInstance::SymbolInstance(SymbolInstance& symbolInstance) noexcept : Instance(symbolInstance) {
	this->setHeight(symbolInstance.getHeight());
	this->setWidth(symbolInstance.getWidth());
	this->setFirstFrame(symbolInstance.getFirstFrame());
	this->setLastFrame(symbolInstance.getLastFrame());
	this->setLoop(symbolInstance.getLoop());
}
const std::string& SymbolInstance::getSymbolType() const noexcept {
	return this->symbolType;
}
void SymbolInstance::setSymbolType(const std::string& symbolType) noexcept {
	if (this->Element::root.attribute("symbolType").empty()) this->Element::root.append_attribute("symbolType");
	this->Element::root.attribute("symbolType").set_value(symbolType.c_str());
	this->symbolType = symbolType;
}
unsigned int SymbolInstance::getFirstFrame() const noexcept {
	return this->firstFrame;
}
void SymbolInstance::setFirstFrame(unsigned int firstFrame) noexcept {
	if (firstFrame == 0) this->Element::root.remove_attribute("firstFrame");
	else {
		if (this->Element::root.attribute("firstFrame").empty()) this->Element::root.append_attribute("firstFrame");
		this->Element::root.attribute("firstFrame").set_value(firstFrame);
	}
	this->firstFrame = firstFrame;
}
std::optional<unsigned int> SymbolInstance::getLastFrame() const noexcept {
	return this->lastFrame;
}
void SymbolInstance::setLastFrame(std::optional<unsigned int> lastFrame) noexcept {
	if (!lastFrame) this->Element::root.remove_attribute("lastFrame");
	else {
		if (this->Element::root.attribute("lastFrame").empty()) this->Element::root.append_attribute("lastFrame");
		this->Element::root.attribute("lastFrame").set_value(lastFrame.value());
	}
	this->lastFrame = lastFrame;
}
const std::string& SymbolInstance::getLoop() const noexcept {
	return this->loop;
}
void SymbolInstance::setLoop(const std::string& loop) noexcept {
	if (this->Element::root.attribute("loop").empty()) this->Element::root.append_attribute("loop");
	this->Element::root.attribute("loop").set_value(loop.c_str());
	this->loop = loop;
}
double SymbolInstance::getWidth() const noexcept {
	// if width is not UNDEF, return it
	if (std::abs(this->width - UNDEF) < std::numeric_limits<double>::epsilon()) return this->width;
	// else, use private implementation and then set it (todo)
	return 0;
}
double SymbolInstance::getHeight() const noexcept {
	// if height is not UNDEF, return it
	if (std::abs(this->height - UNDEF) < std::numeric_limits<double>::epsilon()) return this->height;
	// else, use private implementation and then set it (todo)
	return 0;
}

double SymbolInstance::getWidthRecur() const noexcept {
	// TODO: use a recursive function to go inside symbol, get the smallest width that contains all elements on the current frame, and return it
	return UNDEF;
}
double SymbolInstance::getHeightRecur() const noexcept {
	return UNDEF;
}
