#ifndef FRAME_H
#define FRAME_H
#include "pugixml.hpp"
#include "Element.h"
#include "SymbolInstance.h"
#include <vector>
#include <memory>
enum KeyMode {
	KEY_MODE_NORMAL = 9728,
	KEY_MODE_CLASSIC_TWEEN = 22017,
	KEY_MODE_SHAPE_TWEEN = 17922,
	KEY_MODE_MOTION_TWEEN = 8195,
	KEY_MODE_SHAPE_LAYERS = 8192
};
class Frame {
	friend class Layer;
private:
	pugi::xml_node root;
	std::vector<std::unique_ptr<Element>> elements;
	void loadElements(pugi::xml_node& frameNode) noexcept;
	unsigned int startFrame, duration, keyMode;
	std::string labelType, name;
	void setDuration(unsigned int duration) noexcept;
	void setStartFrame(unsigned int startFrame) noexcept;
	void setKeyMode(unsigned int keyMode) noexcept;
	void clearElements() noexcept;
public:
	Frame(pugi::xml_node& frameNode, bool isBlank = false) noexcept;
	Frame(const Frame& frame, bool isBlank = false) noexcept;
	~Frame() noexcept;
	Element* getElement(unsigned int index) const noexcept;
	unsigned int getDuration() const noexcept;
	unsigned int getStartFrame() const noexcept;
	unsigned int getKeyMode() const noexcept;
	const std::string& getLabelType() const noexcept;
	void setLabelType(const std::string& labelType) noexcept(false);
	const std::string& getName() const noexcept;
	void setName(const std::string& name) noexcept;
	bool isEmpty() const noexcept;
	pugi::xml_node& getRoot() noexcept;
	const pugi::xml_node& getRoot() const noexcept;
};
const std::vector<std::string_view> ACCEPTABLE_LABEL_TYPES = { "none", "name", "comment", "anchor" };
#endif // FRAME_H